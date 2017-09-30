using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class StagingStructuredBufferManager<T> : IDisposable where T : struct {
	private Buffer Buffer { get; }
	public T[] Array { get; }

	public StagingStructuredBufferManager(Device device, int elementCount) {
		int elementSizeInBytes = Marshal.SizeOf<T>();

		Buffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);
		Array = new T[elementCount];
	}
	
	public void Dispose() {
		Buffer.Dispose();
	}

	public void CopyToStagingBuffer(DeviceContext context, Buffer sourceBuffer) {
		context.CopyResource(sourceBuffer, Buffer);
	}

	public void FillArayFromStagingBuffer(DeviceContext context) {
		DataBox dataBox = context.MapSubresource(Buffer, 0, MapMode.Read, MapFlags.None, out DataStream dataStream);
		try {
			dataStream.ReadRange(Array, 0, Array.Length);
		} finally {
			context.UnmapSubresource(Buffer, 0);
			dataStream.Dispose();
		}
	}
}
