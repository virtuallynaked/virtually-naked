using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class StagingStructuredBufferManager<T> : IDisposable where T : struct {
	private Buffer buffer;

	private T[][] arrays;
	private int nextArrayIdx;

	public StagingStructuredBufferManager(Device device, int elementCount, int arrayCount = 1) {
		int elementSizeInBytes = Marshal.SizeOf<T>();

		buffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);

		arrays = new T[arrayCount][];
		for (int i = 0; i < arrayCount; ++i) {
			arrays[i] = new T[elementCount];
		}
	}
	
	public void Dispose() {
		buffer.Dispose();
	}

	public void CopyToStagingBuffer(DeviceContext context, Buffer sourceBuffer) {
		context.CopyResource(sourceBuffer, buffer);
	}

	public T[] FillArrayFromStagingBuffer(DeviceContext context) {
		T[] array = arrays[nextArrayIdx];
		nextArrayIdx = (nextArrayIdx + 1) % arrays.Length;
		
		DataBox dataBox = context.MapSubresource(buffer, 0, MapMode.Read, MapFlags.None, out DataStream dataStream);
		try {
			dataStream.ReadRange(array, 0, array.Length);
		} finally {
			context.UnmapSubresource(buffer, 0);
			dataStream.Dispose();
		}
		
		return array;
	}
}
