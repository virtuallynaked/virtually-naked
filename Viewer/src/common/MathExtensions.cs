using SharpDX;

public static class MathExtensions {
	public static double Sqr(double d) {
		return d * d;
	}

	public static double Cube(double d) {
		return d * d * d;
	}

	public static float Sqr(float d) {
		return d * d;
	}

	public static float Cube(float d) {
		return d * d * d;
	}

	public static Quaternion Pow(this Quaternion q, float f) {
		return Quaternion.RotationAxis(q.Axis, f * q.Angle);
	}

	/*
	 * Return a quaternion q such that v.q == (v.q1).q2, where '.' is Transformation
	 */
	public static Quaternion Chain(this Quaternion q1, Quaternion q2) {
		return q2 * q1;
	}

	public static double Clamp(double x, double min, double max) {
		if (x < min) {
			return min;
		} else if (x > max) {
			return max;
		} else {
			return x;
		}
	}

	public static float Clamp(float x, float min, float max) {
		if (x < min) {
			return min;
		} else if (x > max) {
			return max;
		} else {
			return x;
		}
	}
}
