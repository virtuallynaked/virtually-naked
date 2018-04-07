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

public class FaceTransparencyProcessor: IDisposable {
private static InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
	};

	[StructLayout(LayoutKind.Sequential)]
	private struct OpacityCounters {
		public uint pixelCount;
		public uint opacityCount;
	}
	
	private readonly Device device;
	private readonly Figure figure;

	private int faceCount;
	private readonly float[] faceTransparencies;

	private readonly InputLayout inputLayout;
	private readonly VertexShader vertexShader;
	private readonly PixelShader pixelShader;
	private readonly States states;

	public FaceTransparencyProcessor(Device device, ShaderCache shaderCache, Figure figure) {
		this.device = device;
		this.figure = figure;

		faceCount = figure.Geometry.Faces.Length;
		faceTransparencies = new float[faceCount];

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<TextureMaskRenderer>("occlusion/facetransparency/OpacityCounting");
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, MeshBuffers.InputElements);
		vertexShader = vertexShaderAndBytecode;
		
		pixelShader = shaderCache.GetPixelShader<TextureMaskRenderer>("occlusion/facetransparency/OpacityCounting");

		var statesDesc = StateDescriptions.Default();
		statesDesc.rasterizer.CullMode = CullMode.None;
		states = new States(device, statesDesc);
	}
	
	public void Dispose() {
		inputLayout.Dispose();
		states.Dispose();
	}

	public float[] FaceTransparencies => faceTransparencies;

	private Texture2D LoadOpacityTexture(FileInfo file, bool isLinear) {
		using (var image = UnmanagedRgbaImage.Load(file)) {
			var desc = new Texture2DDescription {
				Width = image.Size.Width,
				Height = image.Size.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = isLinear ? Format.R8G8B8A8_UNorm : Format.R8G8B8A8_UNorm_SRgb,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.ShaderResource
			};

			return new Texture2D(device, desc, image.DataRectangle);
		}
	}

	private void ProcessTexturedSurface(int surfaceIdx, string uvSetName, FileInfo file, bool isLinear) {
		var context = device.ImmediateContext;

		var opacityTexture = LoadOpacityTexture(file, isLinear);
		var opacityTextureView = new ShaderResourceView(device, opacityTexture);

		// Not sure by, possibly a nvidia bug, but the vertex buffer padding by an extra element is necessary.
		// Otherwise (0,0) gets passed to the vertex shader instead of the last vertex.
		var uvSet = figure.UvSets[uvSetName];
		var vertices = uvSet.Uvs;
		var vertexBufferSizeInBytes = Vector2.SizeInBytes * (vertices.Length + 1); 
		var vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices, vertexBufferSizeInBytes);

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
			
			if (!face.IsDegeneratedIntoTriangle) {
				triangleIndices.Add(face.Index2);
				triangleIndices.Add(face.Index3);
				triangleIndices.Add(face.Index0);
			}
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
		vertexBuffer.Dispose();
		opacityCounterBufferManager.Dispose();
		opacityCounterStagingBufferManager.Dispose();
	}

	public void ProcessConstantSurface(int surfaceIdx, float opacity) {
		float transparency = 1 - opacity;

		for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
			if (figure.Geometry.SurfaceMap[faceIdx] != surfaceIdx) {
				continue;
			}

			faceTransparencies[faceIdx] = transparency;
		}
	}

	public void ProcessSurface(int surfaceIdx, string uvSet, RawFloatTexture opacityTexture) {
		if (opacityTexture.image == null) {
			ProcessConstantSurface(surfaceIdx, opacityTexture.value);
		} else {
			bool isLinear = TextureImporter.IsLinear(opacityTexture.image, TextureProcessingType.SingleChannel);
			ProcessTexturedSurface(surfaceIdx, uvSet, opacityTexture.image.file, isLinear);
		}
	}
}
