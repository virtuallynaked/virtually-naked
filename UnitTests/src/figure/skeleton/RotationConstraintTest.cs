using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class RotationConstraintTest {
	private RotationConstraint constraint = new RotationConstraint(RotationOrder.XYZ, new Vector3(-10, -20, -30), new Vector3(15, 25, 35));
	
	private void TestClampRotation(Vector3 input, Vector3 expected) {
		var inputQ = constraint.RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(input));
		var clampedQ = constraint.ClampRotation(inputQ);
		var expectedQ = constraint.RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(expected));
		MathAssert.AreEqual(expectedQ, clampedQ, 1e-4f);
	}

	[TestMethod]
	public void TestClampZero() {
		TestClampRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
	}
	
	[TestMethod]
	public void TestClampTwistOnly() {
		TestClampRotation(new Vector3(-9, 0, 0), new Vector3(-9, 0, 0));
		TestClampRotation(new Vector3(-10, 0, 0), new Vector3(-10, 0, 0));
		TestClampRotation(new Vector3(-11, 0, 0), new Vector3(-10, 0, 0));

		TestClampRotation(new Vector3(+14, 0, 0), new Vector3(+14, 0, 0));
		TestClampRotation(new Vector3(+15, 0, 0), new Vector3(+15, 0, 0));
		TestClampRotation(new Vector3(+16, 0, 0), new Vector3(+15, 0, 0));
	}

	[TestMethod]
	public void TestClampSwingOnly() {
		TestClampRotation(new Vector3(0, -19, 0), new Vector3(0, -19, 0));
		TestClampRotation(new Vector3(0, -20, 0), new Vector3(0, -20, 0));
		TestClampRotation(new Vector3(0, -21, 0), new Vector3(0, -20, 0));

		TestClampRotation(new Vector3(0, +24, 0), new Vector3(0, +24, 0));
		TestClampRotation(new Vector3(0, +25, 0), new Vector3(0, +25, 0));
		TestClampRotation(new Vector3(0, +26, 0), new Vector3(0, +25, 0));

		TestClampRotation(new Vector3(0, 0, -29), new Vector3(0, 0, -29));
		TestClampRotation(new Vector3(0, 0, -30), new Vector3(0, 0, -30));
		TestClampRotation(new Vector3(0, 0, -41), new Vector3(0, 0, -30));

		TestClampRotation(new Vector3(0, 0, +34), new Vector3(0, 0, +34));
		TestClampRotation(new Vector3(0, 0, +35), new Vector3(0, 0, +35));
		TestClampRotation(new Vector3(0, 0, +36), new Vector3(0, 0, +35));
	}

	[TestMethod]
	public void TestClampSwingWithTwist() {
		TestClampRotation(new Vector3(10, -19, 0), new Vector3(10, -19, 0));
		TestClampRotation(new Vector3(10, -20, 0), new Vector3(10, -20, 0));
		TestClampRotation(new Vector3(10, -21, 0), new Vector3(10, -20, 0));

		TestClampRotation(new Vector3(10, +24, 0), new Vector3(10, +24, 0));
		TestClampRotation(new Vector3(10, +25, 0), new Vector3(10, +25, 0));
		TestClampRotation(new Vector3(10, +26, 0), new Vector3(10, +25, 0));

		TestClampRotation(new Vector3(10, 0, -29), new Vector3(10, 0, -29));
		TestClampRotation(new Vector3(10, 0, -30), new Vector3(10, 0, -30));
		TestClampRotation(new Vector3(10, 0, -41), new Vector3(10, 0, -30));

		TestClampRotation(new Vector3(10, 0, +34), new Vector3(10, 0, +34));
		TestClampRotation(new Vector3(10, 0, +35), new Vector3(10, 0, +35));
		TestClampRotation(new Vector3(10, 0, +36), new Vector3(10, 0, +35));
	}

	[TestMethod]
	public void TestClampTwistWithSwing() {
		TestClampRotation(new Vector3(-9, 10, 0), new Vector3(-9, 10, 0));
		TestClampRotation(new Vector3(-10, 10, 0), new Vector3(-10, 10, 0));
		TestClampRotation(new Vector3(-11, 10, 0), new Vector3(-10, 10, 0));

		TestClampRotation(new Vector3(+14, 10, 0), new Vector3(+14, 10, 0));
		TestClampRotation(new Vector3(+15, 10, 0), new Vector3(+15, 10, 0));
		TestClampRotation(new Vector3(+16, 10, 0), new Vector3(+15, 10, 0));
	}

	[TestMethod]
	public void TestClampSwingAndTwist() {
		TestClampRotation(new Vector3(20, 30, 0), new Vector3(15, 25, 0));
		TestClampRotation(new Vector3(20, 0, 40), new Vector3(15, 0, 35));
	}
}