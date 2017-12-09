using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class RotationOrderTest {
	private const float Acc = 1e-4f;
	
	private Quaternion MakeExpectedQ(RotationOrder order, Vector3 angles) {
		Quaternion[] axisRotations = {
			Quaternion.RotationAxis(Vector3.UnitX, angles.X),
			Quaternion.RotationAxis(Vector3.UnitY, angles.Y),
			Quaternion.RotationAxis(Vector3.UnitZ, angles.Z)
		};
		Quaternion expectedQ = axisRotations[order.primaryAxis]
			.Chain(axisRotations[order.secondaryAxis])
			.Chain(axisRotations[order.tertiaryAxis]);
		return expectedQ;
	}

	private void TestFromEulerAngles(RotationOrder order) {
		Vector3 angles = new Vector3(0.1f, 0.2f, 0.3f);

		Quaternion expectedQ = MakeExpectedQ(order, angles);
		Quaternion actualQ = order.FromEulerAngles(angles);

		Assert.AreEqual(expectedQ.W, actualQ.W, Acc, "w differs");
		Assert.AreEqual(expectedQ.X, actualQ.X, Acc, "x differs");
		Assert.AreEqual(expectedQ.Y, actualQ.Y, Acc, "y differs");
		Assert.AreEqual(expectedQ.Z, actualQ.Z, Acc, "z differs");
	}

	private void TestToEulerAngles(RotationOrder order, Vector3 expectedAngles) {
		Quaternion q = MakeExpectedQ(order, expectedAngles);

		Vector3 actualAngles = order.ToEulerAngles(q);
		Assert.AreEqual(expectedAngles.X, actualAngles.X, Acc, "x differs");
		Assert.AreEqual(expectedAngles.Y, actualAngles.Y, Acc, "y differs");
		Assert.AreEqual(expectedAngles.Z, actualAngles.Z, Acc, "z differs");

		//check that negated quaternion gives same result
		actualAngles = order.ToEulerAngles(-q);
		Assert.AreEqual(expectedAngles.X, actualAngles.X, Acc, "x differs");
		Assert.AreEqual(expectedAngles.Y, actualAngles.Y, Acc, "y differs");
		Assert.AreEqual(expectedAngles.Z, actualAngles.Z, Acc, "z differs");
	}
	
	private void TestToSmallEulerAngles(RotationOrder order) {
		TestToEulerAngles(order, new Vector3(+0.1f, +0.2f, +0.3f));

		TestToEulerAngles(order, new Vector3(-0.1f, +0.2f, +0.3f));
		TestToEulerAngles(order, new Vector3(+0.1f, -0.2f, +0.3f));
		TestToEulerAngles(order, new Vector3(+0.1f, +0.2f, -0.3f));

		TestToEulerAngles(order, new Vector3(+0.1f, -0.2f, -0.3f));
		TestToEulerAngles(order, new Vector3(-0.1f, +0.2f, -0.3f));
		TestToEulerAngles(order, new Vector3(-0.1f, -0.2f, +0.3f));

		TestToEulerAngles(order, new Vector3(-0.1f, -0.2f, -0.3f));
	}

	private void TestToLargeSecondaryEulerAngle(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);

		expectedAngles[order.primaryAxis] = -0.1f;
		expectedAngles[order.tertiaryAxis] = -0.1f;
		expectedAngles[order.secondaryAxis] = MathUtil.Pi - 0.1f;
		TestToEulerAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = +0.1f;
		expectedAngles[order.tertiaryAxis] = +0.1f;
		expectedAngles[order.secondaryAxis] = MathUtil.Pi - 0.1f;
		TestToEulerAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = -0.1f;
		expectedAngles[order.tertiaryAxis] = -0.1f;
		expectedAngles[order.secondaryAxis] = -MathUtil.Pi + 0.1f;
		TestToEulerAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = +0.1f;
		expectedAngles[order.tertiaryAxis] = +0.1f;
		expectedAngles[order.secondaryAxis] = -MathUtil.Pi + 0.1f;
		TestToEulerAngles(order, expectedAngles);
	}

	private void TestToEulerAnglesAtPositiveSingularity(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);
		expectedAngles[order.secondaryAxis] = MathUtil.PiOverTwo;
		expectedAngles[order.primaryAxis] = 0;

		expectedAngles[order.tertiaryAxis] = +0.2f;
		TestToEulerAngles(order, expectedAngles);

		expectedAngles[order.tertiaryAxis] = -0.2f;
		TestToEulerAngles(order, expectedAngles);
	}

	private void TestToEulerAnglesAtNegativeSingularity(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);
		expectedAngles[order.secondaryAxis] = -MathUtil.PiOverTwo;
		expectedAngles[order.primaryAxis] = 0;

		expectedAngles[order.tertiaryAxis] = +0.2f;
		TestToEulerAngles(order, expectedAngles);

		expectedAngles[order.tertiaryAxis] = -0.2f;
		TestToEulerAngles(order, expectedAngles);
	}
	
	
	private void TestFromTwistSwingAngles(RotationOrder order) {
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(0, 0, 0)), Quaternion.Identity, Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(+1, 0, 0)), Quaternion.RotationAxis(Vector3.UnitX, +1), Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(-1, 0, 0)), Quaternion.RotationAxis(Vector3.UnitX, -1), Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(0, +1, 0)), Quaternion.RotationAxis(Vector3.UnitY, +1), Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(0, -1, 0)), Quaternion.RotationAxis(Vector3.UnitY, -1), Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(0, 0, +1)), Quaternion.RotationAxis(Vector3.UnitZ, +1), Acc);
		MathAssert.AreEqual(order.FromTwistSwingAngles(new Vector3(0, 0, -1)), Quaternion.RotationAxis(Vector3.UnitZ, -1), Acc);
	}
	
	private void TestToTwistSwingAngles(RotationOrder order) {
		MathAssert.AreEqual(new Vector3(0, 0, 0), order.ToTwistSwingAngles(Quaternion.Identity), Acc);
		MathAssert.AreEqual(new Vector3(+1, 0, 0), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitX, +1)), Acc);
		MathAssert.AreEqual(new Vector3(-1, 0, 0), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitX, -1)), Acc);
		MathAssert.AreEqual(new Vector3(0, +1, 0), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitY, +1)), Acc);
		MathAssert.AreEqual(new Vector3(0, -1, 0), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitY, -1)), Acc);
		MathAssert.AreEqual(new Vector3(0, 0, +1), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitZ, +1)), Acc);
		MathAssert.AreEqual(new Vector3(0, 0, -1), order.ToTwistSwingAngles(Quaternion.RotationAxis(Vector3.UnitZ, -1)), Acc);
	}

	private void TestTwistSwingRoundTrip(RotationOrder order, Quaternion q) {
		MathAssert.AreEqual(q, order.FromTwistSwingAngles(order.ToTwistSwingAngles(q)), Acc);
	}

	private void TestTwistSwingRoundTrip(RotationOrder order) {
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(+0.1f, +0.2f, +0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(-0.1f, +0.2f, +0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(+0.1f, -0.2f, +0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(-0.1f, -0.2f, +0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(+0.1f, +0.2f, -0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(-0.1f, +0.2f, -0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(+0.1f, -0.2f, -0.3f));
		TestTwistSwingRoundTrip(order, Quaternion.RotationYawPitchRoll(-0.1f, -0.2f, -0.3f));
	}

	private void TestRotationOrder(RotationOrder order) {
		TestFromEulerAngles(order);
		TestToSmallEulerAngles(order);
		TestToLargeSecondaryEulerAngle(order);
		TestToEulerAnglesAtPositiveSingularity(order);
		TestToEulerAnglesAtNegativeSingularity(order);

		TestFromTwistSwingAngles(order);
		TestToTwistSwingAngles(order);
		TestTwistSwingRoundTrip(order);
	}

	[TestMethod]
	public void TestXYZ() {
		TestRotationOrder(RotationOrder.XYZ);
	}

	[TestMethod]
	public void TestXZY() {
		TestRotationOrder(RotationOrder.XZY);
	}

	[TestMethod]
	public void TestYXZ() {
		TestRotationOrder(RotationOrder.YXZ);
	}
	
	[TestMethod]
	public void TestYZX() {
		TestRotationOrder(RotationOrder.YZX);
	}

	[TestMethod]
	public void TestZXY() {
		TestRotationOrder(RotationOrder.ZXY);
	}

	[TestMethod]
	public void TestZYX() {
		TestRotationOrder(RotationOrder.ZYX);
	}
}
