using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;
using static SharpDX.MathUtil;

[TestClass]
public class SwingTest {
    private const float Acc = 1e-4f;

    [TestMethod]
    public void TestToQuaternion() {
		MathAssert.AreEqual(Quaternion.Identity, new Swing(0, 0).AsQuaternion(Swing.XAxis), Acc);
		MathAssert.AreEqual(Quaternion.Identity, new Swing(0, 0).AsQuaternion(Swing.YAxis), Acc);
		MathAssert.AreEqual(Quaternion.Identity, new Swing(0, 0).AsQuaternion(Swing.ZAxis), Acc);

		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitY, 1), new Swing(1, 0).AsQuaternion(Swing.XAxis), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitZ, 1), new Swing(1, 0).AsQuaternion(Swing.YAxis), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitX, 1), new Swing(1, 0).AsQuaternion(Swing.ZAxis), Acc);

		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitZ, 1), new Swing(0, 1).AsQuaternion(Swing.XAxis), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitX, 1), new Swing(0, 1).AsQuaternion(Swing.YAxis), Acc);
		MathAssert.AreEqual(Quaternion.RotationAxis(Vector3.UnitY, 1), new Swing(0, 1).AsQuaternion(Swing.ZAxis), Acc);
    }

	private static Vector3 MakeRandomUnitVector(Random rnd) {
		return Vector3.Normalize(new Vector3(rnd.NextFloat(-1, 1), rnd.NextFloat(-1, 1), rnd.NextFloat(-1, 1)));
	}

	[TestMethod]
	public void TestFromTo() {
		var rnd = new Random(0);
		Vector3 a = MakeRandomUnitVector(rnd);
		Vector3 b = MakeRandomUnitVector(rnd);
		
		for (int twistAxis = 0; twistAxis < 3; ++twistAxis) {
			Swing fromAToB = Swing.FromTo(twistAxis, a, b);
			MathAssert.AreEqual(b, Vector3.Transform(a, fromAToB.AsQuaternion(twistAxis)), Acc);

			Swing fromBToA = Swing.FromTo(twistAxis, b, a);
			MathAssert.AreEqual(a, Vector3.Transform(b, fromBToA.AsQuaternion(twistAxis)), Acc);
		}
	}
}
