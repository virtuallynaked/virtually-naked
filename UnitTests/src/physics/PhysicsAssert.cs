using Microsoft.VisualStudio.TestTools.UnitTesting;

public static class PhysicsAssert {
	public static void AreEqual(MassMoment expected, MassMoment actual, float delta) {
		Assert.AreEqual(expected.Mass, actual.Mass, delta, "Mass");
		MathAssert.AreEqual(expected.MassPosition, actual.MassPosition, delta);
		Assert.AreEqual(expected.InertiaXX, actual.InertiaXX, delta, "InertiaXX");
		Assert.AreEqual(expected.InertiaYY, actual.InertiaYY, delta, "InertiaYY");
		Assert.AreEqual(expected.InertiaZZ, actual.InertiaZZ, delta, "InertiaZZ");
		Assert.AreEqual(expected.InertiaXY, actual.InertiaXY, delta, "InertiaXY");
		Assert.AreEqual(expected.InertiaXZ, actual.InertiaXZ, delta, "InertiaXZ");
		Assert.AreEqual(expected.InertiaYZ, actual.InertiaYZ, delta, "InertiaYZ");
	}
}