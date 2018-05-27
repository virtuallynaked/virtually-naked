using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using SharpDX.Direct3D;

public class NormalMapRenderer : IDisposable {
	private struct VertexInfo {
		public static readonly int SizeInBytes = Vector2.SizeInBytes + 3 * Vector3.SizeInBytes;

		public Vector2 uv;
		public Vector3 ldNormal;
		public Vector3 ldTangent;
		public Vector3 hdNormal;
	}

	private static readonly InputElement[] InputElements = new[] {
        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0 * 4, 0), //uv
		new InputElement("NORMAL", 0, Format.R32G32B32_Float, 2 * 4, 0), //ldNormal
		new InputElement("TANGENT", 0, Format.R32G32B32_Float, 5 * 4, 0), //ldNormal
		new InputElement("NORMAL", 1, Format.R32G32B32_Float, 8 * 4, 0), //hdTangent
    };

	private const int Size = 4096;

	private readonly Device device;
	private readonly Vector3[] hdNormals;
	private readonly Vector3[] ldNormals;
	private readonly Quad[] faces;
	private readonly Vector2[] uvs;
	private readonly Vector3[] ldTangents;
	private readonly Quad[] uvFaces;
	private readonly int[] surfaceMap;

	private readonly InputLayout inputLayout;
	private readonly VertexShader vertexShader;
	private readonly PixelShader pixelShader;
	private readonly RasterizerState rasterizerState;
	private readonly Texture2D texture;
	private readonly RenderTargetView textureTargetView;
	private readonly Texture2D resolveTexture;
	private readonly Texture2D stagingTexture;

	public NormalMapRenderer(Device device, ShaderCache shaderCache,
		Vector3[] hdNormals, Vector3[] ldNormals, Quad[] faces,
		Vector2[] uvs, Vector3[] ldTangents, Quad[] uvFaces, int[] surfaceMap) {
		this.device = device;
		this.hdNormals = hdNormals;
		this.ldNormals = ldNormals;
		this.faces = faces;
		this.uvs = uvs;
		this.ldTangents = ldTangents;
		this.uvFaces = uvFaces;
		this.surfaceMap = surfaceMap;

		var vertexShaderAndByteCode = shaderCache.GetVertexShader<NormalMapRenderer>("morphing/hd/NormalMapRenderer");
		inputLayout = new InputLayout(device, vertexShaderAndByteCode.Bytecode, InputElements);
		vertexShader = vertexShaderAndByteCode.Shader;
		pixelShader = shaderCache.GetPixelShader<NormalMapRenderer>("morphing/hd/NormalMapRenderer");

		var rasterizerStateDesc = RasterizerStateDescription.Default();
		rasterizerStateDesc.CullMode = CullMode.None;
		rasterizerState = new RasterizerState(device, rasterizerStateDesc);
		
		var textureDesc = new Texture2DDescription {
			Width = 4096,
			Height = 4096,
			MipLevels = 1,
			ArraySize = 1,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(8, 0),
			BindFlags = BindFlags.RenderTarget
		};
		texture = new Texture2D(device, textureDesc);
		textureTargetView = new RenderTargetView(device, texture);

		var resolveTextureDesc = textureDesc;
		resolveTextureDesc.SampleDescription = new SampleDescription(1, 0);
		resolveTexture = new Texture2D(device, resolveTextureDesc);

		var stagingTextureDesc = resolveTextureDesc;
		stagingTextureDesc.BindFlags = BindFlags.None;
		stagingTextureDesc.Usage = ResourceUsage.Staging;
		stagingTextureDesc.CpuAccessFlags = CpuAccessFlags.Read;
		stagingTexture = new Texture2D(device, stagingTextureDesc);
	}

	public void Dispose() {
		inputLayout.Dispose();
		rasterizerState.Dispose();
		texture.Dispose();
		textureTargetView.Dispose();
		resolveTexture.Dispose();
		stagingTexture.Dispose();
	}

	private static void CopyDataBox(DataBox source, DataBox dest) {
		var minPitch = Math.Min(source.RowPitch, dest.RowPitch);
		for (int sourceOffset = 0, destOffset = 0; sourceOffset < source.SlicePitch; sourceOffset += source.RowPitch, destOffset += dest.RowPitch) {
			Utilities.CopyMemory(dest.DataPointer + destOffset, source.DataPointer + sourceOffset, minPitch);
		}
	}

	private UnmanagedRgbaImage DownloadImageFromGpu() {
		var context = device.ImmediateContext;
		context.CopyResource(resolveTexture, stagingTexture);

		var image = new UnmanagedRgbaImage(new Size2(Size, Size));
		var resultImageData = context.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None);
		CopyDataBox(resultImageData, image.DataBox);
		context.UnmapSubresource(stagingTexture, 0);

		return image;
	}

	private VertexInfo[] MakeVertexInfos(HashSet<int> surfaceIdxs) {
		List<VertexInfo> vertexInfos = new List<VertexInfo>();
		for (int faceIdx = 0; faceIdx < surfaceMap.Length; ++faceIdx) {
			if (!surfaceIdxs.Contains(surfaceMap[faceIdx])) {
				continue;
			}

			var face = faces[faceIdx];
			var uvFace = uvFaces[faceIdx];

			var faceVertexInfos = Enumerable.Range(0, Quad.SideCount)
				.Select(i => new VertexInfo {
					uv = uvs[uvFace.GetCorner(i)],
					ldNormal = ldNormals[face.GetCorner(i)],
					ldTangent = ldTangents[uvFace.GetCorner(i)],
					hdNormal = hdNormals[face.GetCorner(i)],
				})
				.ToArray();

			vertexInfos.Add(faceVertexInfos[0]);
			vertexInfos.Add(faceVertexInfos[1]);
			vertexInfos.Add(faceVertexInfos[2]);
			
			if (!face.IsDegeneratedIntoTriangle) {
				vertexInfos.Add(faceVertexInfos[2]);
				vertexInfos.Add(faceVertexInfos[3]);
				vertexInfos.Add(faceVertexInfos[0]);
			}
		}

		return vertexInfos.ToArray();
	}

	public UnmanagedRgbaImage Render(HashSet<int> surfaceIdxs) {
		var context = device.ImmediateContext;
		context.ClearRenderTargetView(textureTargetView, new Color4(0.5f, 0.5f, 1f, 1));
		
		VertexInfo[] vertexInfos = MakeVertexInfos(surfaceIdxs);
		var vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexInfos);

		context.ClearState();

		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		context.InputAssembler.InputLayout = inputLayout;
		context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexInfo.SizeInBytes, 0));
		context.VertexShader.Set(vertexShader);
		context.Rasterizer.State = rasterizerState;
		context.Rasterizer.SetViewport(0, 0, Size, Size);
		context.PixelShader.Set(pixelShader);
		context.OutputMerger.SetRenderTargets(textureTargetView);

		context.Draw(vertexInfos.Length, 0);

		context.ClearState();

		vertexBuffer.Dispose();

		context.ResolveSubresource(texture, 0, resolveTexture, 0, Format.B8G8R8A8_UNorm);

		return DownloadImageFromGpu();
	}
}
