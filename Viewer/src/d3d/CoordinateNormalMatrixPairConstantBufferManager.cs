using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;

class CoordinateNormalMatrixPairConstantBufferManager : IDisposable {
	private readonly SharpDX.Direct3D11.Buffer buffer;

	public SharpDX.Direct3D11.Buffer Buffer => buffer;

	public CoordinateNormalMatrixPairConstantBufferManager(Device device) {
		this.buffer = new SharpDX.Direct3D11.Buffer(device, Matrix.SizeInBytes * 2, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
	}

	public void Dispose() {
		buffer.Dispose();
	}
	
	public void Update(DeviceContext context, Matrix coordinateMatrix) {
		// Transpose the matrix because the GPU expects in to be in column-major order
		coordinateMatrix.Transpose();

		// The normal matrix is the inverse of the transpose of the top-left of the coordinate matrix.
		// We've already taken the transpose of the coordinate matrix, so we only need to invert.
		Matrix3x3 normalMatrix3 = (Matrix3x3) coordinateMatrix;
		normalMatrix3.Invert();

		// Again, transpose because the GPU expected column-major order
		normalMatrix3.Transpose();
		
		// Convert to a 4x4 matrix for consistency with GPU layout of float3x3
		Matrix normalMatrix = (Matrix) normalMatrix3;

		DataBox dataBox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
		try {
			Marshal.StructureToPtr(coordinateMatrix, dataBox.DataPointer + Matrix.SizeInBytes * 0, false);
			Marshal.StructureToPtr(normalMatrix, dataBox.DataPointer + Matrix.SizeInBytes * 1, false);
		} finally {
			context.UnmapSubresource(buffer, 0);
		}
	}
}
