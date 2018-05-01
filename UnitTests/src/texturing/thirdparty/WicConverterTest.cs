using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.WIC;

[TestClass]
public class WicConverterTest {
	private const byte Srgb50Intensity = 188;

	[TestMethod]
	public void TestThatUint16ToUint8ConversionDoesNotChangeColorSpace() {
		using (var factory = new ImagingFactory()) {
			ushort[] pixelData = new ushort[] { 0, ushort.MaxValue / 2, ushort.MaxValue};
			using (var bitmap = Bitmap.New(factory, 1, 1, PixelFormat.Format48bppRGB, pixelData, sizeof(ushort) * 3)) {
				using (var converter = new FormatConverter(factory)) {
					converter.Initialize(bitmap, PixelFormat.Format32bppRGB);

					byte[] pixelDataOut = new byte[4];
					converter.CopyPixels(pixelDataOut, sizeof(byte) * 4);

					Assert.AreEqual(0, pixelDataOut[0], "r");
					Assert.AreEqual(byte.MaxValue / 2, pixelDataOut[1], "b");
					Assert.AreEqual(byte.MaxValue, pixelDataOut[2], "b");
				}
			}
		}
	}

	[TestMethod]
	public void TestThatFloat32ToUint8ConversionDoesChangeColorSpace() {
		using (var factory = new ImagingFactory()) {
			float[] pixelData = new float[] { 0, 0.5f, 1};
			using (var bitmap = Bitmap.New(factory, 1, 1, PixelFormat.Format96bppRGBFloat, pixelData, sizeof(float) * 3)) {
				using (var converter = new FormatConverter(factory)) {
					converter.Initialize(bitmap, PixelFormat.Format32bppRGB);

					byte[] pixelDataOut = new byte[4];
					converter.CopyPixels(pixelDataOut, sizeof(byte) * 4);

					Assert.AreEqual(0, pixelDataOut[0], "r");
					Assert.AreEqual(Srgb50Intensity, pixelDataOut[1], "b");
					Assert.AreEqual(byte.MaxValue, pixelDataOut[2], "b");
				}
			}
		}
	}
}
