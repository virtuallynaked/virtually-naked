using System;
using static MathExtensions;

public static class EllipseClamp {
	private const int MaxIter = 3;
	
	private static void Normalize(ref double x, ref double y) {
		double m = Math.Sqrt(1 / (Sqr(x) + Sqr(y)));
		x *= m;
		y *= m;
	}

	private static void ClosestPointOnEllipse(double a, double b, double gx, double gy, out double x, out double y) {
		double cos = b * gx;
		double sin = a * gy;
		Normalize(ref cos, ref sin);

		for (int i = 0; i < MaxIter; ++i) {
			double f = b*cos*gy + a*a*cos*sin - b*b*cos*sin - a*gx*sin;
			double fdx = (a*a - b*b)*(cos*cos - sin*sin) - a*cos*gx - b*gy*sin;
			double step = -f / fdx;
			
			double oldCos = cos;
			cos -= step * sin;
			sin += step * oldCos;
			Normalize(ref cos, ref sin);
		}

		x = a * cos;
		y = b * sin;
	}

	private static void ClampToEllipseFirstQuadrant(ref float x, ref float y, float limitX, float limitY) {
		if (limitX <= 0 && limitY <= 0) {
			x = 0;
			y = 0;
		} else if (limitX <= 0) {
			x = 0;
			y = Math.Min(y, limitY);
		} else if (limitY <= 0) {
			x = Math.Min(x, limitX);
			y = 0;
		} else {
			float scaledNorm = MathExtensions.Sqr(x / limitX) + MathExtensions.Sqr(y / limitY);
			if (scaledNorm <= 1) {
				//inside of ellipse
				return;
			}

			ClosestPointOnEllipse(limitX, limitY, x, y, out double clampedX, out double clampedY);
			x = (float) clampedX;
			y = (float) clampedY;
		}
	}

	public static void ClampToEllipse(ref float x, ref float y, float minX, float maxX, float minY, float maxY) {
		float absX, limitX;
		if (x >= 0) {
			limitX = maxX;
			absX = x;
		} else {
			absX = -x;
			limitX = -minX;
		}

		float absY, limitY;
		if (y >= 0) {
			limitY = maxY;
			absY = y;
		} else {
			absY = -y;
			limitY = -minY;
		}

		ClampToEllipseFirstQuadrant(ref absX, ref absY, limitX, limitY);

		x = Math.Sign(x) * absX;
		y = Math.Sign(y) * absY;
	}
}
