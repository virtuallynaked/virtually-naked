using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class TwistTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestAsQuaternion() {
		MathAssert.AreEqual(Quaternion.Identity, new Twist(0).AsQuaternion(CartesianAxis.X), Acc);
		MathAssert.AreEqual(Quaternion.Identity, new Twist(0).AsQuaternion(CartesianAxis.Y), Acc);
		MathAssert.AreEqual(Quaternion.Identity, new Twist(0).AsQuaternion(CartesianAxis.Z), Acc);

		float sinHalfOne = (float) Math.Sin(0.5);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitX, 1), new Twist(sinHalfOne).AsQuaternion(CartesianAxis.X), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitY, 1), new Twist(sinHalfOne).AsQuaternion(CartesianAxis.Y), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitZ, 1), new Twist(sinHalfOne).AsQuaternion(CartesianAxis.Z), Acc);

		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitX, -1), new Twist(-sinHalfOne).AsQuaternion(CartesianAxis.X), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitY, -1), new Twist(-sinHalfOne).AsQuaternion(CartesianAxis.Y), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitZ, -1), new Twist(-sinHalfOne).AsQuaternion(CartesianAxis.Z), Acc);
	}

	[TestMethod]
	public void TestMakeFromAngle() {
		float angle = 0.8f;

		var twist = Twist.MakeFromAngle(angle);

		Assert.AreEqual(angle, twist.Angle, Acc);

		var expectedQ = Quaternion.RotationAxis(Vector3.UnitZ, angle);
		var q = twist.AsQuaternion(CartesianAxis.Z);
		MathAssert.AreEqual(expectedQ, q, Acc);
	}
	
	[TestMethod]
	public void TestApplyDelta() {
		var initialTwist = Twist.MakeFromAngle(0.5f);
		var deltaTwist = Twist.MakeFromAngle(0.3f);
		var finalTwist = Twist.ApplyDelta(initialTwist, deltaTwist);
		MathAssert.AreEqual(Twist.MakeFromAngle(0.8f), finalTwist, Acc);
	}

	[TestMethod]
	public void TestCalculateDelta() {
		var initialTwist = Twist.MakeFromAngle(0.2f);
		var finalTwist = Twist.MakeFromAngle(0.8f);
		var deltaTwist = Twist.CalculateDelta(initialTwist, finalTwist);
		MathAssert.AreEqual(Twist.MakeFromAngle(0.6f), deltaTwist, Acc);
	}
}