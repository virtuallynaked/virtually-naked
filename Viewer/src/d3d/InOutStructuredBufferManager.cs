using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class InOutStructuredBufferManager<T> : IDisposable where T : struct {
	private static readonly int elementSizeInBytes = Marshal.SizeOf<T>();

	public Buffer Buffer { get; }
	public ShaderResourceView InView { get; }
	public UnorderedAccessView OutView { get; }

	public InOutStructuredBufferManager(Device device, int elementCount) {
		Buffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Default, BindFlags.UnorderedAccess | BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);
		InView = new ShaderResourceView(device, Buffer);
		OutView = new UnorderedAccessView(device, Buffer);
	}
	
	public void Dispose() {
		Buffer.Dispose();
		InView.Dispose();
		OutView.Dispose();
	}

	public void Update(DeviceContext context, T[] data, int offset) {
		ResourceRegion region = new ResourceRegion {
			Left = elementSizeInBytes * offset,
			Top = 0,
			Front = 0,

			Right = elementSizeInBytes * (offset + data.Length),
			Bottom = 1,
			Back = 1
		};
		context.UpdateSubresource(data, Buffer, 0, 0, 0, region);
	}
}
