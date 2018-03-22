using SharpDX;
using System;

public static class ColorUtils {
	public static float SrgbToLinear(float s) {
		/*
		if (s <= 0.04045f) {
			return s / 12.92f;
		} else {
			return (float) Math.Pow((s + 0.055) / 1.055, 2.4);
		}
		*/
		return (float) Math.Pow(s, 2.2);
	}

	public static Vector3 SrgbToLinear(Vector3 srgbColor) {
		return new Vector3(SrgbToLinear(srgbColor.X), SrgbToLinear(srgbColor.Y), SrgbToLinear(srgbColor.Z));
	}

	public static Vector3 Log(Vector3 v) {
		return new Vector3(
			(float) Math.Log(v.X),
			(float) Math.Log(v.Y),
			(float) Math.Log(v.Z)
		);
	}
}
