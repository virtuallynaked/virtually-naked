using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SharpDX;

class AnalyzeDazOutputApp {
	public void Run() {
		string filename = @"C:\Users\Ted\Documents\DAZ 3D\Studio\Render Library\out.png";
		Bitmap image = (Bitmap) System.Drawing.Image.FromFile(filename, false);

		Vector3 total = Vector3.Zero;
		int count = 0;
		for (int y = 0; y < image.Width; ++y) {
			for (int x = 0; x < image.Width; ++x) {
				System.Drawing.Color pixel = image.GetPixel(x, y);
				Vector3 pixelVector = new Vector3(pixel.R, pixel.G, pixel.B);
				total += pixelVector;
				count += 1;
			}
		}
		Vector3 mean = total / count / 255;

		if (mean.X >= 0.99f || mean.Y >= 0.99f || mean.Z >= 0.99f) {
			Console.WriteLine("clipped");
		}

		Vector3 normalizationFactor = new Vector3(1, 1, 1);
		mean /= normalizationFactor;
		
		double baseExposure = 13;
		double exposure = 13;
		mean *= (float) Math.Pow(2,exposure - baseExposure);

		Console.WriteLine(mean.X);
		Console.WriteLine(mean.Y);
		Console.WriteLine(mean.Z);
	}
}
