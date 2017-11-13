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
}