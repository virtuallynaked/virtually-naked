using SharpDX;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public class UnmanagedRgbaImage : IDisposable {
	private static readonly HashSet<Guid> SupportedInputPixelFormats = new HashSet<Guid> {
		PixelFormat.Format24bppRGB,
		PixelFormat.Format24bppBGR,
		PixelFormat.Format32bppRGB,
		PixelFormat.Format32bppBGR,
		PixelFormat.Format32bppRGBA,
		PixelFormat.Format32bppBGRA,
		PixelFormat.Format8bppGray,

		PixelFormat.Format48bppBGR,
		PixelFormat.Format48bppRGB,
	};

	public static UnmanagedRgbaImage Load(FileInfo file) {
		using (var factory = new ImagingFactory())
		using (var decoder = new BitmapDecoder(factory, file.FullName, new DecodeOptions { }))
		using (var frame = decoder.GetFrame(0)) {
			if (!SupportedInputPixelFormats.Contains(frame.PixelFormat)) {
				throw new InvalidOperationException($"unsupported pixel format: {frame.PixelFormat}");
			}

			using (var converter = new FormatConverter(factory)) {
				converter.Initialize(frame, PixelFormat.Format32bppBGR);
				
				var size = converter.Size;
				var image = new UnmanagedRgbaImage(size);
				converter.CopyPixels(image.Stride, image.PixelData, image.SizeInBytes);
				return image;
			}
		}
	}

	public void Save(FileInfo file) {
		using (var factory = new ImagingFactory())
		using (var bitmap = new Bitmap(factory, Size.Width, Size.Height, PixelFormat.Format32bppBGR, DataRectangle))
		using (var stream = file.OpenWrite())
		using (var encoder = new BitmapEncoder(factory, ContainerFormatGuids.Png, stream)) {
			using (var frameEncode = new BitmapFrameEncode(encoder)) {
				frameEncode.Initialize();
				frameEncode.WriteSource(bitmap);
				frameEncode.Commit();
			}
			encoder.Commit();
		}
	}

	public const int BytesPerPixel = 4;

	public Size2 Size { get; }
	public IntPtr PixelData { get; }

	public UnmanagedRgbaImage(Size2 size) {
		Size = size;
		PixelData = Marshal.AllocHGlobal(SizeInBytes);
	}

	public int Stride => Size.Width * BytesPerPixel;
	public int SizeInBytes => Size.Height * Stride;

	public DataBox DataBox => new DataBox(PixelData, Stride, SizeInBytes);
	public DataRectangle DataRectangle => new DataRectangle(PixelData, Stride);

	public uint this[int index] {
		get {
			return Utilities.Read<uint>(PixelData + index * BytesPerPixel);
		}
		set {
			Utilities.Write<uint>(PixelData + index * BytesPerPixel, ref value);
		}
	}

	public void Dispose() {
		Marshal.FreeHGlobal(PixelData);
	}
}
