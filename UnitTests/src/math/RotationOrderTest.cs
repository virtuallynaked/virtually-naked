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

	private void TestFromAngles(RotationOrder order) {
		Vector3 angles = new Vector3(0.1f, 0.2f, 0.3f);

		Quaternion expectedQ = MakeExpectedQ(order, angles);
		Quaternion actualQ = order.FromAngles(angles);

		Assert.AreEqual(expectedQ.W, actualQ.W, Acc, "w differs");
		Assert.AreEqual(expectedQ.X, actualQ.X, Acc, "x differs");
		Assert.AreEqual(expectedQ.Y, actualQ.Y, Acc, "y differs");
		Assert.AreEqual(expectedQ.Z, actualQ.Z, Acc, "z differs");
	}

	private void TestToAngles(RotationOrder order, Vector3 expectedAngles) {
		Quaternion q = MakeExpectedQ(order, expectedAngles);

		Vector3 actualAngles = order.ToAngles(q);
		Assert.AreEqual(expectedAngles.X, actualAngles.X, Acc, "x differs");
		Assert.AreEqual(expectedAngles.Y, actualAngles.Y, Acc, "y differs");
		Assert.AreEqual(expectedAngles.Z, actualAngles.Z, Acc, "z differs");

		//check that negated quaternion gives same result
		actualAngles = order.ToAngles(-q);
		Assert.AreEqual(expectedAngles.X, actualAngles.X, Acc, "x differs");
		Assert.AreEqual(expectedAngles.Y, actualAngles.Y, Acc, "y differs");
		Assert.AreEqual(expectedAngles.Z, actualAngles.Z, Acc, "z differs");
	}
	
	private void TestToSmallAngles(RotationOrder order) {
		TestToAngles(order, new Vector3(+0.1f, +0.2f, +0.3f));

		TestToAngles(order, new Vector3(-0.1f, +0.2f, +0.3f));
		TestToAngles(order, new Vector3(+0.1f, -0.2f, +0.3f));
		TestToAngles(order, new Vector3(+0.1f, +0.2f, -0.3f));

		TestToAngles(order, new Vector3(+0.1f, -0.2f, -0.3f));
		TestToAngles(order, new Vector3(-0.1f, +0.2f, -0.3f));
		TestToAngles(order, new Vector3(-0.1f, -0.2f, +0.3f));

		TestToAngles(order, new Vector3(-0.1f, -0.2f, -0.3f));
	}

	private void TestToLargeSecondaryAngle(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);

		expectedAngles[order.primaryAxis] = -0.1f;
		expectedAngles[order.tertiaryAxis] = -0.1f;
		expectedAngles[order.secondaryAxis] = MathUtil.Pi - 0.1f;
		TestToAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = +0.1f;
		expectedAngles[order.tertiaryAxis] = +0.1f;
		expectedAngles[order.secondaryAxis] = MathUtil.Pi - 0.1f;
		TestToAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = -0.1f;
		expectedAngles[order.tertiaryAxis] = -0.1f;
		expectedAngles[order.secondaryAxis] = -MathUtil.Pi + 0.1f;
		TestToAngles(order, expectedAngles);

		expectedAngles[order.primaryAxis] = +0.1f;
		expectedAngles[order.tertiaryAxis] = +0.1f;
		expectedAngles[order.secondaryAxis] = -MathUtil.Pi + 0.1f;
		TestToAngles(order, expectedAngles);
	}

	private void TestPositiveSingularity(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);
		expectedAngles[order.secondaryAxis] = MathUtil.PiOverTwo;
		expectedAngles[order.primaryAxis] = 0;

		expectedAngles[order.tertiaryAxis] = +0.2f;
		TestToAngles(order, expectedAngles);

		expectedAngles[order.tertiaryAxis] = -0.2f;
		TestToAngles(order, expectedAngles);
	}

	private void TestNegativeSingularity(RotationOrder order) {
		Vector3 expectedAngles = default(Vector3);
		expectedAngles[order.secondaryAxis] = -MathUtil.PiOverTwo;
		expectedAngles[order.primaryAxis] = 0;

		expectedAngles[order.tertiaryAxis] = +0.2f;
		TestToAngles(order, expectedAngles);

		expectedAngles[order.tertiaryAxis] = -0.2f;
		TestToAngles(order, expectedAngles);
	}
		
	private void TestRotationOrder(RotationOrder order) {
		TestFromAngles(order);
		TestToSmallAngles(order);
		TestToLargeSecondaryAngle(order);
		TestPositiveSingularity(order);
		TestNegativeSingularity(order);
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
