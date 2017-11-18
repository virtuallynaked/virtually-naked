using SharpDX;
using System;

public class Vector2Utils {
	public static float Atan(Vector2 v) {
		return (float) Math.Atan2(v.Y, v.X);
	}

	public static float AngleBetween(Vector2 from, Vector2 to) {
		float angleDelta = Atan(to) - Atan(from);
		return (float) Math.IEEERemainder(angleDelta, Math.PI * 2);
	}

	public static Vector2 RotateBy(float rotation, Vector2 v) {
		return Matrix3x2.TransformPoint(Matrix3x2.Rotation(rotation), v);
	}
}