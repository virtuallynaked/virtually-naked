using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class StageableStructuredBufferManager<T> : IDisposable where T : struct {
	private readonly int elementCount;

	private readonly Buffer buffer;
	private readonly UnorderedAccessView view;
	private readonly Buffer stagingBuffer;

	public StageableStructuredBufferManager(Device device, int elementCount) {
		this.elementCount = elementCount;

		int elementSizeInBytes = Marshal.SizeOf<T>();

		buffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Default, BindFlags.UnorderedAccess, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);
		view = new UnorderedAccessView(device, buffer);
		stagingBuffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);
	}

	public UnorderedAccessView View => view;

	public void Dispose() {
		buffer.Dispose();
		view.Dispose();
		stagingBuffer.Dispose();
	}

	public T[] ReadContents(DeviceContext context) {
		context.CopyResource(buffer, stagingBuffer);

		DataBox dataBox = context.MapSubresource(stagingBuffer, 0, MapMode.Read, MapFlags.None, out DataStream dataStream);
		try {
			T[] elements = dataStream.ReadRange<T>(elementCount);
			return elements;
		} finally {
			context.UnmapSubresource(stagingBuffer, 0);
			dataStream.Dispose();
		}
	}

	public void WriteContents(DeviceContext context, T[] data) {
		context.UpdateSubresource(data, buffer);
	}
}
