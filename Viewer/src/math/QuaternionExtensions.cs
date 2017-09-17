using SharpDX;
using System;

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
}
