using SharpDX;

public static class FormatForMathematicaExtensions {
	public static string FormatForMathematica(this Vector3 v) {
		return "{" + v.X + ", " + v.Y + ", " + v.Z + "}";
	}

	public static string FormatForMathematica(this Matrix3x3 m) {
		return "{" + FormatForMathematica(m.Row1) + ", " + FormatForMathematica(m.Row2) + ", " + FormatForMathematica(m.Row3) + "}";
	}

	public static string FormatForMathematica(this Quaternion q) {
		return $"{{{q.W}, {q.X}, {q.Y}, {q.Z}}}";
	}
}
