using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class OrientationSpaceTest {
	[TestMethod]
	public void TestDecomposeIntoSwingThenTwistInOrientedSpace() {
		Quaternion orientation = Quaternion.RotationYawPitchRoll(0.4f, 0.3f, 0.2f);
		OrientationSpace orientationSpace = new OrientationSpace(orientation);

		Quaternion q = Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f);

		orientationSpace.DecomposeIntoTwistThenSwing(Vector3.UnitX, q, out var twist, out var swing);
		
		Quaternion twistThenSwing = twist.Chain(swing);
		Assert.AreEqual(twistThenSwing, q);
	}
}
