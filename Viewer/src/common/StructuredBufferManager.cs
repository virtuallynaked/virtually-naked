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

		if (count > 0) {
			//buffers cannot have size 0, but I want to hide this detail from StructuredBufferManager consumers

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
	}

	public void Dispose() {
		buffer?.Dispose();
		view?.Dispose();
	}

	public ShaderResourceView View => view;

	public void Update(DeviceContext context, T[] data) {
		if (buffer != null) {
			context.UpdateSubresource(data, buffer);
		}
	}
}
