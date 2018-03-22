using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using Valve.VR;
using Buffer = SharpDX.Direct3D11.Buffer;

public class HiddenAreaMesh : IDisposable {
	public static readonly InputElement[] InputElements = new [] {
		new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32_Float, 0)
	};

	public static HiddenAreaMesh Make(Device device, EVREye eye) {
		HiddenAreaMesh_t hiddenAreaMeshDefinition = OpenVR.System.GetHiddenAreaMesh(eye, EHiddenAreaMeshType.k_eHiddenAreaMesh_Standard);

		int triangleCount = (int) hiddenAreaMeshDefinition.unTriangleCount;
		if (triangleCount == 0) {
			return null;
		}

		int vertexCount = triangleCount * 3;

		Buffer vertexBuffer = new Buffer(device, hiddenAreaMeshDefinition.pVertexData, new BufferDescription {
			SizeInBytes = vertexCount * Vector2.SizeInBytes,
			BindFlags = BindFlags.VertexBuffer,
			Usage = ResourceUsage.Immutable
		});

		VertexBufferBinding vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Vector2.SizeInBytes, 0);

		return new HiddenAreaMesh(vertexCount, vertexBufferBinding);
	}

	private readonly int vertexCount;
	private readonly VertexBufferBinding vertexBufferBinding;

	public HiddenAreaMesh(int vertexCount, VertexBufferBinding vertexBufferBinding) {
		this.vertexCount = vertexCount;
		this.vertexBufferBinding = vertexBufferBinding;
	}

	public void Dispose() {
		vertexBufferBinding.Buffer.Dispose();
	}

	public void Draw(DeviceContext context) {
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
		context.Draw(vertexCount, 0);
	}
}
