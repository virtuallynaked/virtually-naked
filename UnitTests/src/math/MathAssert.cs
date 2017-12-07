using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

public static class MathAssert {
	public static void AreEqual(Quaternion expected, Quaternion actual, float delta) {
		Assert.AreEqual(expected.X, actual.X, delta, "X");
		Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
		Assert.AreEqual(expected.Z, actual.Z, delta, "Z");
		Assert.AreEqual(expected.W, actual.W, delta, "W");
	}

	public static void AreEqual(Vector3 expected, Vector3 actual, float delta) {
		Assert.AreEqual(expected.X, actual.X, delta, "X");
		Assert.AreEqual(expected.Y, actual.Y, delta, "Y");
		Assert.AreEqual(expected.Z, actual.Z, delta, "Z");
	}
}