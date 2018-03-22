using SharpDX;

public static class ColorConversion {
	public static Vector2 FromTemperatureToCIExy(double temperature) {
		double t = temperature;
		double t2 = t * t;
		double t3 = t2 * t;

		double x;
		if (temperature < 4000) {
			x = -0.2661239e9 / t3 - 0.2343580e6 / t2 + 0.8776956e3 / t + 0.179910;
		} else {
			x = -3.0258469e9 / t3 +2.1070379e6 / t2 + 0.2226347e3 / t + 0.240390;
		}

		double x2 = x * x;
		double x3 = x2 * x;

		double y;
		if (temperature < 2200) {
			y = -1.1063814 * x3 - 1.34811020 * x2 + 2.18555832 * x - 0.20219683;
		} else if (temperature < 4000) {
			y = -0.9549476 * x3 - 1.37418593 * x2 + 2.09137015 * x - 0.16748867;
		} else {
			y = +3.0817580 * x3 - 5.87338670 * x2 + 3.75112997 * x - 0.37001483;
		}

		return new Vector2((float) x, (float) y);
	}

	public static Vector3 FromCIExyTOCIEXYZ(Vector2 xy, double Y) {
		double x = xy[0];
		double y = xy[1];

		double X = Y / y * x;
		double Z = Y / y * (1 - x - y);

		return new Vector3((float) X, (float) Y, (float) Z);
	}

	public static Vector3 FromCIEXYZtoLinearSRGB(Vector3 XYZ) {
		float R = Vector3.Dot(XYZ, new Vector3(+3.2404542f, -1.5371385f, -0.4985314f));
		float G = Vector3.Dot(XYZ, new Vector3(-0.9692660f,  1.8760108f, +0.0415560f));
		float B = Vector3.Dot(XYZ, new Vector3(+0.0556434f, -0.2040259f, +1.0572252f));
		return new Vector3(R, G, B);
	}

	public static Vector3 FromTemperatureToLinearSRGB(double temperature) {
		Vector2 xy = ColorConversion.FromTemperatureToCIExy(temperature);
		Vector3 XYZ = ColorConversion.FromCIExyTOCIEXYZ(xy, 1);
		Vector3 RGB = ColorConversion.FromCIEXYZtoLinearSRGB(XYZ);
		return RGB;
	}
}
