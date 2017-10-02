using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Collections.Generic;
using System.IO;

public class FromTextureFaceTransparencyCalculator : IDisposable {
private static InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
	};

	[StructLayout(LayoutKind.Sequential)]
	private struct OpacityCounters {
		public uint pixelCount;
		public uint opacityCount;
	}
	
	private readonly ContentFileLocator fileLocator;
	private readonly Device device;
	private readonly Figure figure;

	private readonly InputLayout inputLayout;
	private readonly VertexShader vertexShader;
	private readonly PixelShader pixelShader;
	private readonly States states;
	
	private readonly Buffer vertexBuffer;
	
	public FromTextureFaceTransparencyCalculator(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure figure) {
		this.fileLocator = fileLocator;
		this.device = device;
		this.figure = figure;

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<TextureMaskRenderer>("occlusion/facetransparency/OpacityCounting");
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, QuadMeshBuffers.InputElements);
		vertexShader = vertexShaderAndBytecode;
		
		pixelShader = shaderCache.GetPixelShader<TextureMaskRenderer>("occlusion/facetransparency/OpacityCounting");

		var statesDesc = StateDescriptions.Default();
		statesDesc.rasterizer.CullMode = CullMode.None;
		states = new States(device, statesDesc);
		
		// Not sure by, possibly a nvidia bug, but the vertex buffer padding by an extra element is necessary.
		// Otherwise (0,0) gets passed to the vertex shader instead of the last vertex.
		var vertices = figure.DefaultUvSet.Uvs;
		var vertexBufferSizeInBytes = Vector2.SizeInBytes * (vertices.Length + 1); 
		vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices, vertexBufferSizeInBytes);

	}
	
	public void Dispose() {
		inputLayout.Dispose();
		states.Dispose();
		vertexBuffer.Dispose();
	}

	private Texture2D LoadOpacityTexture(int surfaceIdx) {
		string contentFileName;
		if (figure.Name == "liv-hair") {
			string surfaceName = figure.Geometry.SurfaceNames[surfaceIdx];
			if (surfaceName == "Cap") {
				contentFileName = "/Runtime/Textures/outoftouch/!hair/OOTHairblending2/Liv/OOTUtilityLivCapT.jpg";
			} else {
				contentFileName = "/Runtime/Textures/outoftouch/!hair/OOTHairblending2/Liv/OOTUtilityLivHairT.png";
			}
		} else {
			throw new Exception("unsupported figure: " + figure.Name);
		}

		FileInfo file = new FileInfo(fileLocator.Locate(contentFileName));

		using (var image = UnmanagedRgbaImage.Load(file)) {
			var desc = new Texture2DDescription {
				Width = image.Size.Width,
				Height = image.Size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.R8G8B8A8_UNorm,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.ShaderResource
			};

			return new Texture2D(device, desc, image.DataRectangle);
		}
	}

	public float[] CalculateSurfaceTransparencies() {
		int faceCount = figure.Geometry.Faces.Length;
		float[] faceTransparencies = new float[faceCount];

		var context = device.ImmediateContext;
		
		for (int surfaceIdx = 0; surfaceIdx < figure.Geometry.SurfaceCount; ++surfaceIdx) {
			var opacityTexture = LoadOpacityTexture(surfaceIdx);
			var opacityTextureView = new ShaderResourceView(device, opacityTexture);

			List<int> faceIdxMap = new List<int>();
			List<int> triangleIndices = new List<int>();
			for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
				if (figure.Geometry.SurfaceMap[faceIdx] != surfaceIdx) {
					continue;
				}

				faceIdxMap.Add(faceIdx);

				Quad face = figure.DefaultUvSet.Faces[faceIdx];
				triangleIndices.Add(face.Index0);
				triangleIndices.Add(face.Index1);
				triangleIndices.Add(face.Index2);
			
				triangleIndices.Add(face.Index2);
				triangleIndices.Add(face.Index3);
				triangleIndices.Add(face.Index0);
			}

			var indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, triangleIndices.ToArray());
			
			var opacityCounterBufferManager = new InOutStructuredBufferManager<OpacityCounters>(device, faceIdxMap.Count);

			context.ClearState();
			
			states.Apply(context);

			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			context.InputAssembler.InputLayout = inputLayout;
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Vector2.SizeInBytes, 0));
			context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

			context.VertexShader.Set(vertexShader);
			
			context.Rasterizer.SetViewport(0, 0, opacityTexture.Description.Width, opacityTexture.Description.Height);

			context.PixelShader.Set(pixelShader);
			context.PixelShader.SetShaderResources(0, opacityTextureView);
			
			context.OutputMerger.SetUnorderedAccessView(0, opacityCounterBufferManager.OutView);

			context.DrawIndexed(triangleIndices.Count, 0, 0);

			context.ClearState();
						
			var opacityCounterStagingBufferManager = new StagingStructuredBufferManager<OpacityCounters>(device, faceIdxMap.Count);
			opacityCounterStagingBufferManager.CopyToStagingBuffer(context, opacityCounterBufferManager.Buffer);
			var array = opacityCounterStagingBufferManager.FillArrayFromStagingBuffer(context);
			
			for (int faceIdx = 0; faceIdx < faceIdxMap.Count; ++faceIdx) {
				OpacityCounters opacityCounter = array[faceIdx];
			
				if (opacityCounter.pixelCount > (1<<24)) {
					throw new Exception("pixel count overflow");
				}

				float opacity = opacityCounter.opacityCount == 0 ? 0 : (float) opacityCounter.opacityCount / opacityCounter.pixelCount / 0xff;
				float transparency = 1 - opacity;
				faceTransparencies[faceIdxMap[faceIdx]] = transparency;
			}
			
			opacityTextureView.Dispose();
			opacityTexture.Dispose();
			indexBuffer.Dispose();
			opacityCounterBufferManager.Dispose();
			opacityCounterStagingBufferManager.Dispose();
		}
		
		return faceTransparencies;
	}
}
