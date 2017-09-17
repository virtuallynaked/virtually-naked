using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;

//Optimized for occasional update (ResourceUsage.Default + UpdateSubresource)
public class StructuredBufferManager<T> : IDisposable where T : struct {
	private readonly Device device;
	private readonly Buffer buffer;
	private readonly ShaderResourceView view;

	public StructuredBufferManager(Device device, int count) {
		this.device = device;

		int elementSizeInBytes = Marshal.SizeOf<T>();

		BufferDescription description = new BufferDescription {
			SizeInBytes = count * elementSizeInBytes,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.ShaderResource,
			CpuAccessFlags = CpuAccessFlags.None,
			OptionFlags = ResourceOptionFlags.BufferStructured,
			StructureByteStride = elementSizeInBytes
		};
		this.buffer = new Buffer(device, description);
		this.view = new ShaderResourceView(device, buffer);
	}

	public void Dispose() {
		buffer.Dispose();
		view.Dispose();
	}

	public ShaderResourceView View => view;

	public void Update(T[] data) {
		device.ImmediateContext.UpdateSubresource(data, buffer);
	}
}
