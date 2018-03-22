using SharpDX;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public class UnmanagedGrayImage : IDisposable {
	private static readonly HashSet<Guid> SupportedInputPixelFormats = new HashSet<Guid> {
		PixelFormat.Format24bppRGB,
		PixelFormat.Format24bppBGR,
		PixelFormat.Format32bppRGB,
		PixelFormat.Format32bppBGR,
		PixelFormat.Format8bppGray
	};

	public static UnmanagedGrayImage Load(FileInfo file) {
		using (var factory = new ImagingFactory())
		using (var decoder = new BitmapDecoder(factory, file.FullName, new DecodeOptions { }))
		using (var frame = decoder.GetFrame(0)) {
			if (!SupportedInputPixelFormats.Contains(frame.PixelFormat)) {
				throw new InvalidOperationException($"unsupported pixel format: {frame.PixelFormat}");
			}

			using (var converter = new FormatConverter(factory)) {
				converter.Initialize(frame, PixelFormat.Format8bppGray);
				
				var size = converter.Size;
				var image = new UnmanagedGrayImage(size);
				converter.CopyPixels(image.Stride, image.PixelData, image.SizeInBytes);
				return image;
			}
		}
	}

	public const int BytesPerPixel = 1;

	public Size2 Size { get; }
	public IntPtr PixelData { get; }

	public UnmanagedGrayImage(Size2 size) {
		Size = size;
		PixelData = Marshal.AllocHGlobal(SizeInBytes);
	}

	public int Stride => Size.Width * BytesPerPixel;
	public int SizeInBytes => Size.Height * Stride;

	public DataBox DataBox => new DataBox(PixelData, Stride, SizeInBytes);

	public byte this[int index] {
		get {
			return Utilities.Read<byte>(PixelData + index * BytesPerPixel);
		}
		set {
			Utilities.Write<byte>(PixelData + index * BytesPerPixel, ref value);
		}
	}

	public void Dispose() {
		Marshal.FreeHGlobal(PixelData);
	}
}
