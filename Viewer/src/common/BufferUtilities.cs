using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class BufferUtilities {
	public static ShaderResourceView ToStructuredBufferView<T>(Device device, T[] array) where T : struct {
		using (Buffer buffer = Buffer.Create(device, BindFlags.ShaderResource, array, usage: ResourceUsage.Immutable, optionFlags: ResourceOptionFlags.BufferStructured, structureByteStride: Marshal.SizeOf<T>())) {
			return new ShaderResourceView(device, buffer);
		}
	}

	public static Buffer ToConstantBuffer<T>(Device device, T data) where T : struct {
		return Buffer.Create(device, BindFlags.ConstantBuffer, ref data, usage: ResourceUsage.Immutable);
	}
}
