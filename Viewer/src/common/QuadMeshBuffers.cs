using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using System;

public class QuadMeshBuffers : IDisposable {
	public static InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32B32_Float, (int) Marshal.OffsetOf<VertexInfo>("position"), 0),
		new InputElement("NORMAL", 0, Format.R32G32B32_Float, (int) Marshal.OffsetOf<VertexInfo>("normal"), 0),
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VertexInfo {
		public static readonly int SizeInBytes = Marshal.SizeOf<VertexInfo>();

		public Vector3 position;
		public Vector3 normal;
	}

	private readonly Buffer vertexBuffer;
	private readonly Buffer indexBuffer;
	private readonly int indexCount;

	public QuadMeshBuffers(Device device, QuadMesh mesh) {
		int vertexCount = mesh.VertexPositions.Count;
		VertexInfo[] vertexInfos = new VertexInfo[vertexCount];
		for (int i = 0; i < vertexCount; ++i) {
			vertexInfos[i] = new VertexInfo {
				position = mesh.VertexPositions[i],
				normal = mesh.VertexNormals[i]
			};
		}
		this.vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexInfos);

		//convert quad faces to triangles
		int faceCount = mesh.Faces.Count;
		int[] indices = new int[faceCount * 6];
		for (int i = 0; i < faceCount; ++i) {
			Quad face = mesh.Faces[i];

			//first triangle
			indices[i * 6 + 0] = face.Index0;
			indices[i * 6 + 1] = face.Index1;
			indices[i * 6 + 2] = face.Index2;

			//second triangle
			indices[i * 6 + 3] = face.Index2;
			indices[i * 6 + 4] = face.Index3;
			indices[i * 6 + 5] = face.Index0;
		}

		this.indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indices);
		this.indexCount = indices.Length;
	}

	public void Dispose() {
		vertexBuffer.Dispose();
		indexBuffer.Dispose();
	}

	public void Bind(InputAssemblerStage inputAssembler) {
		inputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		inputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexInfo.SizeInBytes, 0));
		inputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
	}

	public void Draw(DeviceContext context) {
		context.DrawIndexed(indexCount, 0, 0);
	}
}
