using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SplineTest {
    private const double Acc = 1e-4;

    [TestMethod]
    public void TestPositiveSpline() {
		Spline.Knot[] knots = new [] {
			new Spline.Knot(0f, 0f),
			new Spline.Knot(70f, 1f),
			new Spline.Knot(110f, 1f),
			new Spline.Knot(155.5f, 0f)
		};
		Spline spline = new Spline(knots);
		
        // Test at knots:
        Assert.AreEqual(0, spline.Eval(0), Acc);
        Assert.AreEqual(1, spline.Eval(70), Acc);
        Assert.AreEqual(1, spline.Eval(110), Acc);
        Assert.AreEqual(0, spline.Eval(155.5f), Acc);

        // Test past ends
        Assert.AreEqual(0, spline.Eval(-999), Acc);
        Assert.AreEqual(0, spline.Eval(+999), Acc);

        // Test between knots, numbers are from Daz Studio
        Assert.AreEqual(0.3268f, spline.Eval(30f), Acc);
        Assert.AreEqual(0.8778f, spline.Eval(60f), Acc);
        Assert.AreEqual(1.1039f, spline.Eval(90f), Acc);
        Assert.AreEqual(0.8051f, spline.Eval(120f), Acc);
        Assert.AreEqual(0.0335f, spline.Eval(150f), Acc);
    }
}
