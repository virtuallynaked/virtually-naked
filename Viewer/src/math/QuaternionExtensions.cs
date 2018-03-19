using SharpDX;
using System;
using static SharpDX.Vector3;
using static MathExtensions;
using static System.Math;

public static class QuaternionExtensions {
	public static Quaternion RotateBetween(Vector3 v1, Vector3 v2) {
		Vector3 xyz = Vector3.Cross(v1, v2);
		float w = (float) Math.Sqrt(v1.LengthSquared() * v2.LengthSquared()) + Vector3.Dot(v1, v2);
		Quaternion q = new Quaternion(xyz, w);
		q.Normalize();
		return q;
	}

	public static void DecomposeIntoTwistThenSwing(this Quaternion q, Vector3 axis, out Quaternion twist, out Quaternion swing) {
		//Note: I'm unsure if this works with anything except unit vectors as the axis
		twist = new Quaternion(axis.X * q.X, axis.Y * q.Y, axis.Z * q.Z, q.W);
		twist.Normalize();

		swing = q * Quaternion.Invert(twist);
    }

	public static Quaternion SwingBetween(Vector3 a, Vector3 b, Vector3 n) {
		//Assumes a, b, and n are unit vectors

		Vector3 axis = Normalize(Cross(n, a-b));

		Vector3 rejectionA = a - axis * Dot(a, axis);
		Vector3 rejectionB = b - axis * Dot(b, axis);

		float cosAngle = Dot(Normalize(rejectionA), Normalize(rejectionB));
		float angle = (float) Math.Acos(cosAngle);
		
		Vector3 crossRejection = Cross(rejectionA, rejectionB);
		
		if (Dot(axis, crossRejection) < 0) {
			angle = -angle;
		}

		return Quaternion.RotationAxis(axis, angle);
	}

	//In general, gives the same result as Quaternion.Angle, but is more numerically accurate for angles close to 0
	public static float AccurateAngle(this Quaternion q) {
		double lengthSquared = Sqr((double) q.X) + Sqr((double) q.Y) + Sqr((double) q.Z);
		double angle = 2 * Math.Asin(Sqrt(lengthSquared));
		return (float) angle;
	}

	public static Quaternion FromRotationVector(Vector3 v) {
		Quaternion logQ = new Quaternion(v / 2, 0);
		return Quaternion.Exponential(logQ);
	}

	public static Vector3 ToRotationVector(this Quaternion q) {
		double lengthSquared = Sqr((double) q.X) + Sqr((double) q.Y) + Sqr((double) q.Z);
		double sinAngle = Sqrt(lengthSquared);
		double angle = 2 * Math.Asin(sinAngle);
		double m = Sign(q.W) * angle / sinAngle;
		return new Vector3(
			(float) (m * q.X),
			(float) (m * q.Y),
			(float) (m * q.Z));
	}
}
