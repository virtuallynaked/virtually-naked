using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BreastGravityAnimatorTest {
	[TestMethod]
	public void TestExpandPositive() {
		Assert.AreEqual(-1, BreastGravityAnimator.ExpandPositive(-1), 0);
		Assert.AreEqual( 0, BreastGravityAnimator.ExpandPositive( 0), 0);
		Assert.AreEqual(+2, BreastGravityAnimator.ExpandPositive(+1), 0);
	}

	[TestMethod]
	public void TestExpandNegative() {
		Assert.AreEqual(-2, BreastGravityAnimator.ExpandNegative(-1), 0);
		Assert.AreEqual( 0, BreastGravityAnimator.ExpandNegative( 0), 0);
		Assert.AreEqual(+1, BreastGravityAnimator.ExpandNegative(+1), 0);
	}
}