using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TwistConstraintTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestTest() {
		var constraint = new TwistConstraint(-0.1f, +0.2f);

		Assert.IsTrue(constraint.Test(new Twist(-0.09f)));
		Assert.IsFalse(constraint.Test(new Twist(-0.11f)));

		Assert.IsTrue(constraint.Test(new Twist(+0.19f)));
		Assert.IsFalse(constraint.Test(new Twist(+0.21f)));
	}

	[TestMethod]
	public void TestClamp() {
		var constraint = new TwistConstraint(-0.1f, +0.2f);

		MathAssert.AreEqual(new Twist(-0.09f), constraint.Clamp(new Twist(-0.09f)), Acc);
		MathAssert.AreEqual(new Twist(-0.10f), constraint.Clamp(new Twist(-0.11f)), Acc);

		MathAssert.AreEqual(new Twist(+0.19f), constraint.Clamp(new Twist(+0.19f)), Acc);
		MathAssert.AreEqual(new Twist(+0.20f), constraint.Clamp(new Twist(+0.20f)), Acc);
	}
}
