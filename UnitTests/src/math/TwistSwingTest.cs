using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class TwistSwingTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestAsQuaternion() {
		var twist = new Twist(0.4f);
		var swing = new Swing(0.2f, 0.3f);
		var twistSwing = new TwistSwing(twist, swing);

		MathAssert.AreEqual(
			twist.AsQuaternion(CartesianAxis.X).Chain(swing.AsQuaternion(CartesianAxis.X)),
			twistSwing.AsQuaternion(CartesianAxis.X),
			Acc);

		MathAssert.AreEqual(
			twist.AsQuaternion(CartesianAxis.X).Chain(swing.AsQuaternion(CartesianAxis.X)),
			twistSwing.AsQuaternion(CartesianAxis.X),
			Acc);

		MathAssert.AreEqual(
			twist.AsQuaternion(CartesianAxis.X).Chain(swing.AsQuaternion(CartesianAxis.X)),
			twistSwing.AsQuaternion(CartesianAxis.X),
			Acc);

		MathAssert.AreEqual(
			twist.AsQuaternion(CartesianAxis.X).Chain(swing.AsQuaternion(CartesianAxis.X)),
			twistSwing.AsQuaternion(CartesianAxis.X),
			Acc);
	}

	[TestMethod]
	public void TestDecompose() {
		var q = Quaternion.Normalize(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));

		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.X, q).AsQuaternion(CartesianAxis.X), Acc);
		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.Y, q).AsQuaternion(CartesianAxis.Y), Acc);
		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.Z, q).AsQuaternion(CartesianAxis.Z), Acc);

		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.X, -q).AsQuaternion(CartesianAxis.X), Acc);
		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.Y, -q).AsQuaternion(CartesianAxis.Y), Acc);
		MathAssert.AreEqual(q, TwistSwing.Decompose(CartesianAxis.Z, -q).AsQuaternion(CartesianAxis.Z), Acc);
	}

	[TestMethod]
	public void TestDecomposeHalfRevolution() {
		var q = new Quaternion(1, 0, 0, 0);
		TwistSwing twistSwing = TwistSwing.Decompose(CartesianAxis.Z, q);

		Assert.AreEqual(0, twistSwing.Twist.X, Acc);
		
		Assert.AreEqual(1, twistSwing.Swing.Y, Acc);
		Assert.AreEqual(0, twistSwing.Swing.Z, Acc);
	}
}
