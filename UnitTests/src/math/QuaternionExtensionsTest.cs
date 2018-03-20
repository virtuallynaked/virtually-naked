using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class QuaternionExtensionsTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestDecomposeIntoSwingThenTwistAlongX() {
		Quaternion q = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);
		q.DecomposeIntoTwistThenSwing(Vector3.UnitX, out Quaternion twist, out Quaternion swing);
		
		//check that twist only has X component
		Assert.AreEqual(0, twist.Y);
		Assert.AreEqual(0, twist.Z);

		//check that swing has no X component
		Assert.AreEqual(0, swing.X);

		//check that swing-then-twist is equivalent to original rotation
		Quaternion swingThenTwist = twist.Chain(swing);
		Assert.AreEqual(swingThenTwist, q);
	}

	[TestMethod]
	public void TestDecomposeIntoSwingThenTwistAlongY() {
		Quaternion q = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);
		q.DecomposeIntoTwistThenSwing(Vector3.UnitY, out Quaternion twist, out Quaternion swing);
		
		//check that twist only has Y component
		Assert.AreEqual(0, twist.X);
		Assert.AreEqual(0, twist.Z);

		//check that swing has no Y component
		Assert.AreEqual(0, swing.Y);

		//check that swing-then-twist is equivalent to original rotation
		Quaternion swingThenTwist = twist.Chain(swing);
		Assert.AreEqual(swingThenTwist, q);
	}

	[TestMethod]
	public void TestDecomposeIntoTwistThenSwingAlongArbitraryAxis() {
		Random rnd = new Random(0);
		Quaternion q = RandomUtil.UnitQuaternion(rnd);
		Vector3 axis = RandomUtil.UnitVector3(rnd);

		q.DecomposeIntoTwistThenSwing(axis, out var twist, out var swing);

		//check that twist axis parallel to axis
		Assert.AreEqual(1, Math.Abs(Vector3.Dot(twist.Axis, axis)), Acc);

		//check that swing axis is perpendicular to axis
		Assert.AreEqual(0, Vector3.Dot(swing.Axis, axis), Acc);

		//check that swing-then-twist is equivalent to original rotation
		MathAssert.AreEqual(q, twist.Chain(swing), Acc);
	}

	[TestMethod]
	public void TestRotateBetween() {
		Vector3 v1 = new Vector3(2, 3, 4);
		Vector3 v2 = new Vector3(7, 6, 5);

		Quaternion q = QuaternionExtensions.RotateBetween(v1, v2);

		Assert.AreEqual(
			0,
			Vector3.Distance(
				Vector3.Normalize(Vector3.Transform(v1, q)),
				Vector3.Normalize(v2)),
			1e-6);
	}

	[TestMethod]
	public void TestHlslRotate() {
		Vector3 p = new Vector3(2, 3, 4);
		Quaternion q = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);

		Vector3 expectedResult = Vector3.Transform(p, q);

		Vector3 qXYZ = new Vector3(q.X, q.Y, q.Z);
		Vector3 actualResult = p + 2 * Vector3.Cross(Vector3.Cross(p, qXYZ) - q.W * p, qXYZ);

		Assert.AreEqual(
			0,
			Vector3.Distance(
				expectedResult,
				actualResult),
			1e-6);
	}

	[TestMethod]
	public void TestChain() {
		Quaternion q1 = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);
		Quaternion q2 = Quaternion.RotationYawPitchRoll(0.2f, -0.3f, 0.4f);
		Quaternion q12 = q1.Chain(q2);

		Assert.AreEqual(1, q12.Length(), 1e-4, "chained result is normalized");

		Vector3 v = new Vector3(3, 4, 5);

		var expected = Vector3.Transform(Vector3.Transform(v, q1), q2);
		var actual = Vector3.Transform(v, q1.Chain(q2));

		Assert.AreEqual(0, Vector3.Distance(expected, actual), 1e-4);
	}

	private static Vector3 MakeRandomUnitVector(Random rnd) {
		return Vector3.Normalize(new Vector3(rnd.NextFloat(-1, 1), rnd.NextFloat(-1, 1), rnd.NextFloat(-1, 1)));
	}

	[TestMethod]
	public void TestSwingBetween() {
		var rnd = new Random(0);
		Vector3 a = MakeRandomUnitVector(rnd);
		Vector3 b = MakeRandomUnitVector(rnd);
		Vector3 n = MakeRandomUnitVector(rnd);

		Quaternion q = QuaternionExtensions.SwingBetween(a, b, n);
		
		Vector3 rotatedA = Vector3.Transform(a, q);
		Assert.AreEqual(0, Vector3.Distance(b, rotatedA), 1e-4, "rotation takes a to b");

		Assert.AreEqual(0, Vector3.Dot(q.Axis, n), 1e-4, "rotation axis lies in plane");
	}

	[TestMethod]
	public void TestFromRotationVector() {
		Vector3 v = new Vector3(0.1f, 0.2f, 0.3f);

		var expected = Quaternion.RotationAxis(v, v.Length());
		MathAssert.AreEqual(expected, QuaternionExtensions.FromRotationVector(v), 1e-4f);
		MathAssert.AreEqual(Quaternion.Invert(expected), QuaternionExtensions.FromRotationVector(-v), 1e-4f);
	}

	[TestMethod]
	public void TestToRotationVector() {
		Vector3 v = new Vector3(0.1f, 0.2f, 0.3f);
		var q = Quaternion.RotationAxis(v, v.Length());

		MathAssert.AreEqual(v, q.ToRotationVector(), 1e-4f);
		MathAssert.AreEqual(v, (-q).ToRotationVector(), 1e-4f);
	}

	[TestMethod]
	public void TestAccurateAngle() {
		Quaternion q = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);
		Assert.AreEqual(q.Angle, q.AccurateAngle(), 1e-4f);
	}
}
