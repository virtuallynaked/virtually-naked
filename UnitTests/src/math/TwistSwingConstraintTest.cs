using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class TwistSwingConstraintTest {
	private TwistSwingConstraint constraint = TwistSwingConstraint.MakeFromRadians(
		CartesianAxis.X, new Vector3(-0.10f, -0.20f, -0.30f), new Vector3(0.15f, 0.25f, 0.35f));
	
	private void TestClampRotation(Vector3 input, Vector3 expected) {
		var inputTS = new TwistSwing(Twist.MakeFromAngle(input.X), Swing.MakeFromAxisAngleProduct(input.Y, input.Z));
		var clampedTS = constraint.Clamp(inputTS);
		var expectedTS = new TwistSwing(Twist.MakeFromAngle(expected.X), Swing.MakeFromAxisAngleProduct(expected.Y, expected.Z));

		MathAssert.AreEqual(expectedTS, clampedTS, 1e-4f);
	}

	[TestMethod]
	public void TestClampZero() {
		TestClampRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
	}
	
	[TestMethod]
	public void TestClampTwistOnly() {
		TestClampRotation(new Vector3(-0.09f, 0, 0), new Vector3(-0.09f, 0, 0));
		TestClampRotation(new Vector3(-0.10f, 0, 0), new Vector3(-0.10f, 0, 0));
		TestClampRotation(new Vector3(-0.11f, 0, 0), new Vector3(-0.10f, 0, 0));

		TestClampRotation(new Vector3(+0.14f, 0, 0), new Vector3(+0.14f, 0, 0));
		TestClampRotation(new Vector3(+0.15f, 0, 0), new Vector3(+0.15f, 0, 0));
		TestClampRotation(new Vector3(+0.16f, 0, 0), new Vector3(+0.15f, 0, 0));
	}

	[TestMethod]
	public void TestClampSwingOnly() {
		TestClampRotation(new Vector3(0, -0.19f, 0), new Vector3(0, -0.19f, 0));
		TestClampRotation(new Vector3(0, -0.20f, 0), new Vector3(0, -0.20f, 0));
		TestClampRotation(new Vector3(0, -0.21f, 0), new Vector3(0, -0.20f, 0));

		TestClampRotation(new Vector3(0, +0.24f, 0), new Vector3(0, +0.24f, 0));
		TestClampRotation(new Vector3(0, +0.25f, 0), new Vector3(0, +0.25f, 0));
		TestClampRotation(new Vector3(0, +0.26f, 0), new Vector3(0, +0.25f, 0));

		TestClampRotation(new Vector3(0, 0, -0.29f), new Vector3(0, 0, -0.29f));
		TestClampRotation(new Vector3(0, 0, -0.30f), new Vector3(0, 0, -0.30f));
		TestClampRotation(new Vector3(0, 0, -0.41f), new Vector3(0, 0, -0.30f));

		TestClampRotation(new Vector3(0, 0, +0.34f), new Vector3(0, 0, +0.34f));
		TestClampRotation(new Vector3(0, 0, +0.35f), new Vector3(0, 0, +0.35f));
		TestClampRotation(new Vector3(0, 0, +0.36f), new Vector3(0, 0, +0.35f));
	}

	[TestMethod]
	public void TestClampSwingWithTwist() {
		TestClampRotation(new Vector3(0.10f, -0.19f, 0), new Vector3(0.10f, -0.19f, 0));
		TestClampRotation(new Vector3(0.10f, -0.20f, 0), new Vector3(0.10f, -0.20f, 0));
		TestClampRotation(new Vector3(0.10f, -0.21f, 0), new Vector3(0.10f, -0.20f, 0));

		TestClampRotation(new Vector3(0.10f, +0.24f, 0), new Vector3(0.10f, +0.24f, 0));
		TestClampRotation(new Vector3(0.10f, +0.25f, 0), new Vector3(0.10f, +0.25f, 0));
		TestClampRotation(new Vector3(0.10f, +0.26f, 0), new Vector3(0.10f, +0.25f, 0));

		TestClampRotation(new Vector3(0.10f, 0, -0.29f), new Vector3(0.10f, 0, -0.29f));
		TestClampRotation(new Vector3(0.10f, 0, -0.30f), new Vector3(0.10f, 0, -0.30f));
		TestClampRotation(new Vector3(0.10f, 0, -0.41f), new Vector3(0.10f, 0, -0.30f));

		TestClampRotation(new Vector3(0.10f, 0, +0.34f), new Vector3(0.10f, 0, +0.34f));
		TestClampRotation(new Vector3(0.10f, 0, +0.35f), new Vector3(0.10f, 0, +0.35f));
		TestClampRotation(new Vector3(0.10f, 0, +0.36f), new Vector3(0.10f, 0, +0.35f));
	}

	[TestMethod]
	public void TestClampTwistWithSwing() {
		TestClampRotation(new Vector3(-0.09f, 0.10f, 0), new Vector3(-0.09f, 0.10f, 0));
		TestClampRotation(new Vector3(-0.10f, 0.10f, 0), new Vector3(-0.10f, 0.10f, 0));
		TestClampRotation(new Vector3(-0.11f, 0.10f, 0), new Vector3(-0.10f, 0.10f, 0));

		TestClampRotation(new Vector3(+0.14f, 0.10f, 0), new Vector3(+0.14f, 0.10f, 0));
		TestClampRotation(new Vector3(+0.15f, 0.10f, 0), new Vector3(+0.15f, 0.10f, 0));
		TestClampRotation(new Vector3(+0.16f, 0.10f, 0), new Vector3(+0.15f, 0.10f, 0));
	}

	[TestMethod]
	public void TestClampSwingAndTwist() {
		TestClampRotation(new Vector3(0.20f, 0.30f, 0), new Vector3(0.15f, 0.25f, 0));
		TestClampRotation(new Vector3(0.20f, 0, 0.40f), new Vector3(0.15f, 0, 0.35f));
	}

	[TestMethod]
	public void TestCenter() {
		var constraint = new TwistSwingConstraint(
			new TwistConstraint(-0.1f, 0.3f),
			new SwingConstraint(
				-0.2f, 0.6f,
				-0.4f, 1.0f));

		MathAssert.AreEqual(TwistSwing.MakeFromCoordinates(0.1f, 0.2f, 0.3f), constraint.Center, 1e-4f);
	}
}
