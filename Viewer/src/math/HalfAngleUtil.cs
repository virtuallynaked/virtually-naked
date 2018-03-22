using static System.Math;
using static MathExtensions;

public static class HalfAngleUtil {
	public static float ToQuatCoordinate(float angle) {
		return (float) Sin(angle / 2);
	}

	public static float Add(float w1, float x1, float w2, float x2) {
		float wSum = w1 * w2 - x1 * x2;
		float xSum = w2 * x1 + w1 * x2;

		return wSum >= 0 ? +xSum : -xSum; 
	}

	public static float Add(float x1, float x2) {
		float w1 = (float) Sqrt(1 - Sqr(x1));
		float w2 = (float) Sqrt(1 - Sqr(x2));

		return Add(w1, x1, w2, x2);
	}
}
