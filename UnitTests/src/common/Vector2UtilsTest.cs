using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class Vector2UtilsTest {
	[TestMethod]
	public void TestAngleBetween() {
		Assert.AreEqual(0, Vector2Utils.AngleBetween(+Vector2.UnitX, +Vector2.UnitX), 1e-6f);
		Assert.AreEqual(MathUtil.PiOverFour, Vector2Utils.AngleBetween(+Vector2.UnitX, new Vector2(1, 1)), 1e-6f);

		Assert.AreEqual(MathUtil.PiOverTwo, Vector2Utils.AngleBetween(+Vector2.UnitX, +Vector2.UnitY), 1e-6f);
		Assert.AreEqual(MathUtil.PiOverTwo, Vector2Utils.AngleBetween(+Vector2.UnitY, -Vector2.UnitX), 1e-6f);
		Assert.AreEqual(MathUtil.PiOverTwo, Vector2Utils.AngleBetween(-Vector2.UnitX, -Vector2.UnitY), 1e-6f);
		Assert.AreEqual(MathUtil.PiOverTwo, Vector2Utils.AngleBetween(-Vector2.UnitY, +Vector2.UnitX), 1e-6f);

		Assert.AreEqual(-MathUtil.PiOverTwo, Vector2Utils.AngleBetween(+Vector2.UnitY, +Vector2.UnitX), 1e-6f);
		Assert.AreEqual(-MathUtil.PiOverTwo, Vector2Utils.AngleBetween(-Vector2.UnitX, +Vector2.UnitY), 1e-6f);
		Assert.AreEqual(-MathUtil.PiOverTwo, Vector2Utils.AngleBetween(-Vector2.UnitY, -Vector2.UnitX), 1e-6f);
		Assert.AreEqual(-MathUtil.PiOverTwo, Vector2Utils.AngleBetween(+Vector2.UnitX, -Vector2.UnitY), 1e-6f);

	}
}
