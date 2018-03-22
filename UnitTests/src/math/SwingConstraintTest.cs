using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SwingConstraintTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestClamp_OnAxis() {
		var constraint = new SwingConstraint(-0.1f, +0.2f, -0.3f, +0.4f);
		
		MathAssert.AreEqual(new Swing(-0.09f, 0), constraint.Clamp(new Swing(-0.09f, 0)), Acc);
		MathAssert.AreEqual(new Swing(-0.10f, 0), constraint.Clamp(new Swing(-0.11f, 0)), Acc);
		
		MathAssert.AreEqual(new Swing(+0.19f, 0), constraint.Clamp(new Swing(+0.19f, 0)), Acc);
		MathAssert.AreEqual(new Swing(+0.20f, 0), constraint.Clamp(new Swing(+0.21f, 0)), Acc);

		MathAssert.AreEqual(new Swing(0, -0.29f), constraint.Clamp(new Swing(0, -0.29f)), Acc);
		MathAssert.AreEqual(new Swing(0, -0.30f), constraint.Clamp(new Swing(0, -0.31f)), Acc);
		
		MathAssert.AreEqual(new Swing(0, +0.39f), constraint.Clamp(new Swing(0, +0.39f)), Acc);
		MathAssert.AreEqual(new Swing(0, +0.40f), constraint.Clamp(new Swing(0, +0.41f)), Acc);
	}

	[TestMethod]
	public void TestOffClamp_OffAxis() {
		var constraint = new SwingConstraint(-0.1f, +0.2f, -0.3f, +0.4f);
		var unclampedSwing = new Swing(0.3f, 0.5f);
		var clampedSwing = constraint.Clamp(unclampedSwing);
		
		// NArgMin[{EuclideanDistance[{0.3, 0.5}, {x, y}], (x/0.2)^2 + (y/0.4)^2 <= 1}, {x, y}]
		var expectedClampedSwing = new Swing(0.10463055104437786f, 0.34089557289708267f);
		MathAssert.AreEqual(expectedClampedSwing, clampedSwing, 1e-4f);
	}

	[TestMethod]
	public void TestTest() {
		var constraint = new SwingConstraint(-0.1f, +0.2f, -0.3f, +0.4f);

		Assert.IsTrue(constraint.Test(new Swing(-0.09f, 0)));
		Assert.IsFalse(constraint.Test(new Swing(-0.11f, 0)));

		Assert.IsTrue(constraint.Test(new Swing(+0.19f, 0)));
		Assert.IsFalse(constraint.Test(new Swing(+0.21f, 0)));

		Assert.IsTrue(constraint.Test(new Swing(0, -0.29f)));
		Assert.IsFalse(constraint.Test(new Swing(0, -0.31f)));

		Assert.IsTrue(constraint.Test(new Swing(0, +0.39f)));
		Assert.IsFalse(constraint.Test(new Swing(0, +0.41f)));
	}
}
