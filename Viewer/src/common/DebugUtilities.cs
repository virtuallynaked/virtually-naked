using SharpDX;
using System;
using System.Diagnostics;
using System.Threading;

static class DebugUtilities {
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
	public static void AssertSamePosition(Vector3 v1, Vector3 v2) {
		float distance = Vector3.Distance(v1, v2);
		float denominator = (Vector3.Distance(v1, Vector3.Zero) + Vector3.Distance(v2, Vector3.Zero)) / 2 + 1e-1f;
		float relativeDistance = distance / denominator;
		Debug.Assert(relativeDistance < 1e-2, "not same position");
	}

	[Conditional("DEBUG")]
	public static void AssertIsUnit(Vector3 v) {
		Debug.Assert(v.IsNormalized, "not normalized");
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
}

