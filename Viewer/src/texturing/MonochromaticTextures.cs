using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

public class MonochromaticTextures {
	public static Texture2D Make(Device device, Color color) {
		Color[] colorData = new [] { color };

		GCHandle colorDataHandle = GCHandle.Alloc(colorData, GCHandleType.Pinned);
		try {
			Texture2DDescription desc = new Texture2DDescription {
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				Format = Format.R8G8B8A8_UNorm_SRgb,
				Width = 1,
				Height = 1,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription {
					Count = 1,
				},
				Usage = ResourceUsage.Immutable
			};

			DataRectangle dataRect = new DataRectangle(colorDataHandle.AddrOfPinnedObject(), sizeof(byte) * 4);

			return new Texture2D(device, desc, dataRect);
		}
		finally {
			colorDataHandle.Free();
		}
	}

	public static Texture2D Make(Device device, Vector4 value) {
		Vector4[] valueData = new [] { value };

		GCHandle colorDataHandle = GCHandle.Alloc(valueData, GCHandleType.Pinned);
		try {
			Texture2DDescription desc = new Texture2DDescription {
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				Format = Format.R32G32B32A32_Float,
				Width = 1,
				Height = 1,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription {
					Count = 1,
				},
				Usage = ResourceUsage.Immutable
			};

			DataRectangle dataRect = new DataRectangle(colorDataHandle.AddrOfPinnedObject(), Vector4.SizeInBytes);

			return new Texture2D(device, desc, dataRect);
		}
		finally {
			colorDataHandle.Free();
		}
	}
}
