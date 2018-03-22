using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class MathExtensionsTest {
	[TestMethod]
	public void TestQuaternionPow() {
		Quaternion q = Quaternion.RotationYawPitchRoll(1,2,3);
		
		float alpha = 0.8f;
		Quaternion q1 = q.Pow(alpha);
		Quaternion q2 = q.Pow(1 - alpha);
		Assert.AreEqual(q, q1 * q2);
		Assert.AreEqual(q, q2 * q1);
	}

	[TestMethod]
	public void TestQuaternionChaining() {
		Quaternion q1 = Quaternion.RotationYawPitchRoll(1,2,3);
		Quaternion q2 = Quaternion.RotationYawPitchRoll(2,3,4);

		Vector3 v = new Vector3(3,4,5);

		Vector3 expectedResult = Vector3.Transform(Vector3.Transform(v, q1), q2);
		Vector3 actualResult = Vector3.Transform(v, q1.Chain(q2));

		Assert.IsTrue(Vector3.NearEqual(expectedResult, actualResult, 1e-5f * Vector3.One));
	}

	[TestMethod]
	public void TestCrossProductMatrix() {
		Vector3 v = new Vector3(2, 3, 4);
		Vector3 u = new Vector3(9, 7, 5);

		Matrix3x3 m = MathExtensions.CrossProductMatrix(v);

		MathAssert.AreEqual(Vector3.Cross(v, u), MathExtensions.Mul(m, u), 1e-5f);
	}
}
