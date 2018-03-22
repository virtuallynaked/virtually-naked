using SharpDX;
using static System.Math;

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
	
	/**
	 * Properties:
	 *     f[x,c] = x when x is close to 0
	 *     f[x,c] = 0 when Abs[x] > c
	 */
	public static float TukeysBiweight(float x, float rejectionThreshold) {
		float z = x / rejectionThreshold;

		if (Abs(z) > 1) {
			return 0;
		} else {
			return x * Sqr(1 - Sqr(z));
		}
	}

	public static Vector3 DegreesToRadians(Vector3 angles) {
		return new Vector3(
			MathUtil.DegreesToRadians(angles.X),
			MathUtil.DegreesToRadians(angles.Y),
			MathUtil.DegreesToRadians(angles.Z));
	}

	public static Vector3 RadiansToDegrees(Vector3 angles) {
		return new Vector3(
			MathUtil.RadiansToDegrees(angles.X),
			MathUtil.RadiansToDegrees(angles.Y),
			MathUtil.RadiansToDegrees(angles.Z));
	}

	public static Vector3 Mul(Matrix3x3 m, Vector3 v) {
		return new Vector3(
			m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z,
			m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z,
			m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z);
	}

	//returns a matrix m such that Mul(m, r) == Cross(v, r)
	public static Matrix3x3 CrossProductMatrix(Vector3 v) {
		return new Matrix3x3(
			0, -v.Z, +v.Y,
			+v.Z, 0, -v.X,
			-v.Y, +v.X, 0);
	}
}
