using SharpDX.Direct3D11;
using System;
using SharpDX;
using System.Runtime.InteropServices;

public class ConstantBufferManager<T> : IDisposable where T : struct {
	private static readonly int SizeOfTInBytes =  Marshal.SizeOf<T>();

	private readonly SharpDX.Direct3D11.Buffer buffer;

	public SharpDX.Direct3D11.Buffer Buffer => buffer;

	public ConstantBufferManager(Device device) {
		this.buffer = new SharpDX.Direct3D11.Buffer(device, IntegerUtils.NextLargerMultiple(SizeOfTInBytes, 16), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
	}

	public void Dispose() {
		buffer.Dispose();
	}
	
	public void Update(DeviceContext context, T value) {
		DataBox dataBox = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
		try {
			Marshal.StructureToPtr(value, dataBox.DataPointer, false);
		} finally {
			context.UnmapSubresource(buffer, 0);
		}
	}
}
