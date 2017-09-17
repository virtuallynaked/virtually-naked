using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;

class ViewProjectionConstantBufferManager : IDisposable {
	private readonly SharpDX.Direct3D11.Buffer buffer;

	public SharpDX.Direct3D11.Buffer Buffer => buffer;

	public ViewProjectionConstantBufferManager(Device device) {
		int padding = sizeof(float);
		this.buffer = new SharpDX.Direct3D11.Buffer(device, Matrix.SizeInBytes + Vector3.SizeInBytes + padding, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
	}

	public void Dispose() {
		buffer.Dispose();
	}
	
	public void Update(DeviceContext context, Matrix viewMatrix, Matrix projectionMatrix) {
		Matrix viewProjectionMatrix = viewMatrix * projectionMatrix;
		// Transpose the matrix because the GPU expects in to be in column-major order
		viewProjectionMatrix.Transpose();

		viewMatrix.Invert();
		Vector3 eyePosition = viewMatrix.TranslationVector;

		DataBox dataBox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
		try {
			Marshal.StructureToPtr(viewProjectionMatrix, dataBox.DataPointer, false);
			Marshal.StructureToPtr(eyePosition, dataBox.DataPointer + Matrix.SizeInBytes, false);
		} finally {
			context.UnmapSubresource(buffer, 0);
		}
	}
}
