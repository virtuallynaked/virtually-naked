using System;
using System.Drawing;
using SharpDX;
using System.IO;

class AnalyzeDazColorSwatchesApp : IDemoApp {
	private void FillRectangle(Bitmap bitmap, int left, int top, int width, int height, System.Drawing.Color color) { 
		for (int y = top; y < top + height; ++y) {
			for (int x = left; x < left + width; ++x) {
				bitmap.SetPixel(x, y, color);
			}
		}
	}

	private void PrepareSwatchImage() {
		int size = 256;

		var bitmap = new Bitmap(size * 21, size * 7);
		for (int i = 0; i < 21; ++i) {
			int v = i * 10;

			FillRectangle(bitmap, i * size, 0 * size, size, size, System.Drawing.Color.FromArgb(v / 2, v / 5, v));
			//FillRectangle(bitmap, i * size, 0 * size, size, size, System.Drawing.Color.FromArgb(v, v, v));
			FillRectangle(bitmap, i * size, 1 * size, size, size, System.Drawing.Color.FromArgb(v, 0, 0));
			FillRectangle(bitmap, i * size, 2 * size, size, size, System.Drawing.Color.FromArgb(0, v, 0));
			FillRectangle(bitmap, i * size, 3 * size, size, size, System.Drawing.Color.FromArgb(0, 0, v));
			FillRectangle(bitmap, i * size, 4 * size, size, size, System.Drawing.Color.FromArgb(v, v, 0));
			FillRectangle(bitmap, i * size, 5 * size, size, size, System.Drawing.Color.FromArgb(v, 0, v));
			FillRectangle(bitmap, i * size, 6 * size, size, size, System.Drawing.Color.FromArgb(0, v, v));
		}

		bitmap.Save(@"C:\Users\Ted\Documents\simple images\swatches.png");
	}

	private Vector3 AnalyzeRectangle(Bitmap image, int left, int top, int width, int height) {
		Vector3 total = Vector3.Zero;
		int count = 0;
		for (int y = top; y < top + height; ++y) {
			for (int x = left; x < left + width; ++x) {
				System.Drawing.Color pixel = image.GetPixel(x, y);
				Vector3 pixelVector = new Vector3(pixel.R, pixel.G, pixel.B);
				total += pixelVector;
				count += 1;
			}
		}
		Vector3 mean = total / count / 255;
		
		Vector3 normalizationFactor = new Vector3(1, 1, 1);
		mean /= normalizationFactor;
		
		double baseExposure = 13;
		double exposure = 13;
		mean *= (float) Math.Pow(2,exposure - baseExposure);

		return mean;
	}

	public void Run() {
		//PrepareSwatchImage();

		var writer = new StreamWriter(@"c:\Users\Ted\Desktop\out.csv");

		int size = 64;
		int border = 2;
		string filename = @"C:\Users\Ted\Documents\DAZ 3D\Studio\Render Library\out.png";
		Bitmap image = (Bitmap) System.Drawing.Image.FromFile(filename, false);
		int j = 0;
		for (int i = 0; i < 21; ++i) {
			Vector3 mean = AnalyzeRectangle(image, i * size + border, j * size + border, size - 2 * border, size - 2 * border);
			writer.WriteLine(mean.X + "," + mean.Y + "," + mean.Z);
			Console.WriteLine(mean.X + ",\t" + mean.Y + ",\t" + mean.Z);
		}

		writer.Close();
	}
}
