using SharpDX;
using SharpDX.WIC;
using System.IO;

public class BumpToNormalDemo {
	private int ToIndex(Size2 size, int x, int y) {
		return y * size.Width + x;
	}

	private int ToIndexClamped(Size2 size, int x, int y) {
		return ToIndex(size,
			IntegerUtils.Clamp(x, 0, size.Width - 1),
			IntegerUtils.Clamp(y, 0, size.Height - 1));
	}

	private float DecodeByte(byte b) {
		return b / 255f;
	}

	private static byte EncodeByte(float f) {
		if (f < 0) {
			return 0;
		} else if (f > 1) {
			return 0xff;
		} else {
			return (byte) (0xff * f);
		}
	}

	private static uint EncodeNormal(Vector3 v) {
		byte r = EncodeByte(v.X * 0.5f + 0.5f);
		byte g = EncodeByte(v.Y * 0.5f + 0.5f);
		byte b = EncodeByte(v.Z * 0.5f + 0.5f);

		return (uint) ((r << 16) | (g << 8) | (b << 0)); 
	}

	public void Run() {
		DirectoryInfo dir = new DirectoryInfo(@"C:\Users\Ted\Documents\simple images");
		FileInfo bumpFile = dir.File("bump-demo.png");

		var bumpImage = UnmanagedGrayImage.Load(bumpFile);
		Size2 size = bumpImage.Size;
		
		var normalsImage = new UnmanagedRgbaImage(size);
		
		float strength = 2f;

		for (int y = 0; y < size.Height; ++y) {
			for (int x = 0; x < size.Width; ++x) {
				int index = ToIndex(size, x, y);

				float left = strength * DecodeByte(bumpImage[ToIndexClamped(size, x - 1, y)]);
				float right = strength * DecodeByte(bumpImage[ToIndexClamped(size, x + 1, y)]);

				float up = strength * DecodeByte(bumpImage[ToIndexClamped(size, x, y - 1)]);
				float down = strength * DecodeByte(bumpImage[ToIndexClamped(size, x, y + 1)]);
				
				var normal = new Vector3(left - right, down - up, 1);
				normal.Normalize();

				normalsImage[index] = EncodeNormal(normal);
			}
		}

		FileInfo normalsFile = dir.File("bump-demo-to-normal.png");
		normalsImage.Save(normalsFile);
	}
}
