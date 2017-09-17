using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class IntegerUtilsTest {
	[TestMethod]
	public void TestMod() {
		Assert.AreEqual(0, IntegerUtils.Mod(-3, 3));
		Assert.AreEqual(1, IntegerUtils.Mod(-2, 3));
		Assert.AreEqual(2, IntegerUtils.Mod(-1, 3));
		Assert.AreEqual(0, IntegerUtils.Mod( 0, 3));
		Assert.AreEqual(1, IntegerUtils.Mod( 1, 3));
		Assert.AreEqual(2, IntegerUtils.Mod( 2, 3));
		Assert.AreEqual(0, IntegerUtils.Mod( 3, 3));
		Assert.AreEqual(1, IntegerUtils.Mod( 4, 3));
		Assert.AreEqual(2, IntegerUtils.Mod( 5, 3));
	}

	[TestMethod]
	public void TestNextLargerMultiple() {
		Assert.AreEqual(0, IntegerUtils.NextLargerMultiple( 0, 3));
		Assert.AreEqual(3, IntegerUtils.NextLargerMultiple( 1, 3));
		Assert.AreEqual(3, IntegerUtils.NextLargerMultiple( 2, 3));
		Assert.AreEqual(3, IntegerUtils.NextLargerMultiple( 3, 3));
		Assert.AreEqual(6, IntegerUtils.NextLargerMultiple( 4, 3));
		Assert.AreEqual(6, IntegerUtils.NextLargerMultiple( 5, 3));
	}

	[TestMethod]
	public void TestClamp() {
		Assert.AreEqual(2, IntegerUtils.Clamp(1, 2, 4));
		Assert.AreEqual(2, IntegerUtils.Clamp(2, 2, 4));
		Assert.AreEqual(3, IntegerUtils.Clamp(3, 2, 4));
		Assert.AreEqual(4, IntegerUtils.Clamp(4, 2, 4));
		Assert.AreEqual(4, IntegerUtils.Clamp(5, 2, 4));
	}

	[TestMethod]
	public void TestFromUShort() {
		Assert.AreEqual(0.5 / 0x10000, IntegerUtils.FromUShort(0));
		Assert.AreEqual(1.5 / 0x10000, IntegerUtils.FromUShort(1));
		Assert.AreEqual(1 - 0.5 / 0x10000, IntegerUtils.FromUShort(ushort.MaxValue));
	}

	[TestMethod]
	public void TestToUShort() {
		//below range
		Assert.AreEqual(0, IntegerUtils.ToUShort(-1));

		Assert.AreEqual(0, IntegerUtils.ToUShort(0));
		Assert.AreEqual(0, IntegerUtils.ToUShort(0.999f / 0x10000));

		Assert.AreEqual(1, IntegerUtils.ToUShort(1.001f / 0x10000));
		Assert.AreEqual(1, IntegerUtils.ToUShort(1.999f / 0x10000));

		Assert.AreEqual(ushort.MaxValue, IntegerUtils.ToUShort(1 - 1e-6f));
		Assert.AreEqual(ushort.MaxValue, IntegerUtils.ToUShort(1));

		//above range
		Assert.AreEqual(ushort.MaxValue, IntegerUtils.ToUShort(2));
	}
}
