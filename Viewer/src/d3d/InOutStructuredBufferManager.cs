using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class InOutStructuredBufferManager<T> : IDisposable where T : struct {
	public Buffer Buffer { get; }
	public ShaderResourceView InView { get; }
	public UnorderedAccessView OutView { get; }

	public InOutStructuredBufferManager(Device device, int elementCount) {
		int elementSizeInBytes = Marshal.SizeOf<T>();

		Buffer = new Buffer(device, elementCount * elementSizeInBytes, ResourceUsage.Default, BindFlags.UnorderedAccess | BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, structureByteStride: elementSizeInBytes);
		InView = new ShaderResourceView(device, Buffer);
		OutView = new UnorderedAccessView(device, Buffer);
	}
	
	public void Dispose() {
		Buffer.Dispose();
		InView.Dispose();
		OutView.Dispose();
	}
}
