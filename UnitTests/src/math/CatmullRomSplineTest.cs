using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class CatmullRomSplineTest {
	[TestMethod]
	public void TestWeights() {
		//check endpoints
		Assert.AreEqual(Vector4.UnitY, CatmullRomSpline.GetWeights(0));
		Assert.AreEqual(Vector4.UnitZ, CatmullRomSpline.GetWeights(1));

		//check symmetry
		Vector4 w1 = CatmullRomSpline.GetWeights(1/4f);
		Vector4 w3 = CatmullRomSpline.GetWeights(3/4f);
		Assert.AreEqual(w1.X, w3.W, 1e-4);
		Assert.AreEqual(w1.Y, w3.Z, 1e-4);
		Assert.AreEqual(w1.Z, w3.Y, 1e-4);
		Assert.AreEqual(w1.W, w3.X, 1e-4);
	}
}
