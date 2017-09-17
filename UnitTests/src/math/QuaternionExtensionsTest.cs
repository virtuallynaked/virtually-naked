using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class QuaternionExtensionsTest {
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
}
