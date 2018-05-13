using SharpDX;
using System;
using System.Diagnostics;
using System.Threading;

public static class DebugUtilities {
	public static void Burn(long ms) {
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms) {
		}
	}

	public static void Sleep(long ms) {
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms) {
			Thread.Yield();
		}
	}
	
	[Conditional("DEBUG")]
	public static void AssertSame(float f1, float f2, string message) {
		Debug.Assert(Math.Abs(f1 - f2) / (f1 + f2 + 1e-2) < 1e-2, message);
	}

	[Conditional("DEBUG")]
	public static void AssertSamePosition(Vector3 v1, Vector3 v2) {
		float distance = Vector3.Distance(v1, v2);
		float denominator = (Vector3.Distance(v1, Vector3.Zero) + Vector3.Distance(v2, Vector3.Zero)) / 2 + 1e-1f;
		float relativeDistance = distance / denominator;
		Debug.Assert(relativeDistance < 1e-2, "not same position");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnit(Vector3 v) {
		float lengthSquared = v.LengthSquared();
		Debug.Assert(Math.Abs(lengthSquared - 1) < 1e-2f, "not normalized");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnitOrZero(Vector3 v) {
		float length = v.Length();
		Debug.Assert(MathUtil.IsZero(length) || Math.Abs(length - 1) < 1e-2f, "not normalized or zero");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnit(Vector2 v) {
		float lengthSquared = v.LengthSquared();
		Debug.Assert(Math.Abs(lengthSquared - 1) < 1e-2f, "not normalized");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnit(Quaternion q) {
		float lengthSquared = q.LengthSquared();
		Debug.Assert(Math.Abs(lengthSquared - 1) < 1e-2f, "not normalized");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnit(float x, float y) {
		float lengthSquared = x * x + y * y;
		Debug.Assert(Math.Abs(lengthSquared - 1) < 1e-2f, "not normalized");
	}

	[Conditional("DEBUG")]
	public static void AssertSameDirection(Vector3 v1, Vector3 v2) {
		Vector3 u1 = Vector3.Normalize(v1);
		Vector3 u2 = Vector3.Normalize(v2);
		float dotProduct = Vector3.Dot(u1, u2);
		Debug.Assert(Math.Abs(dotProduct - 1) < 1e-2f, "not same direction");
	}

	[Conditional("DEBUG")]
	public static void AssertSameRotation(Quaternion q1, Quaternion q2) {
		float dotProduct = Quaternion.Dot(q1, q2);
		Debug.Assert(Math.Abs(dotProduct - 1) < 1e-2f, "not same rotation");
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(float f) {
		Debug.Assert(!float.IsNaN(f), "value is NaN");
		Debug.Assert(!float.IsInfinity(f), "value is infinity");
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(Vector3 v) {
		AssertFinite(v.X);
		AssertFinite(v.Y);
		AssertFinite(v.Z);
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(Quaternion q) {
		AssertFinite(q.X);
		AssertFinite(q.Y);
		AssertFinite(q.Z);
		AssertFinite(q.W);
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(Twist t) {
		AssertFinite(t.X);
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(Swing s) {
		AssertFinite(s.Y);
		AssertFinite(s.Z);
	}

	[Conditional("DEBUG")]
	public static void AssertFinite(TwistSwing ts) {
		AssertFinite(ts.Twist);
		AssertFinite(ts.Swing);
	}

	[Conditional("DEBUG")]
	public static void AssertSame(Twist t1, Twist t2) {
		AssertSame(t1.X, t2.X, "not same twist");
	}

	[Conditional("DEBUG")]
	public static void AssertSame(Swing s1, Swing s2) {
		AssertSame(s1.Y, s2.Y, "not same swing");
		AssertSame(s1.Z, s2.Z, "not same swing");
	}

	[Conditional("DEBUG")]
	public static void AssertSame(TwistSwing ts1, TwistSwing ts2) {
		AssertSame(ts1.Twist, ts2.Twist);
		AssertSame(ts1.Swing, ts2.Swing);
	}
}
