using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class EllipseClampTest {
    private const float Acc = 1e-4f;

    private void AssertClampedResult(float expectedX, float expectedY, float x, float y, float minX, float maxX, float minY, float maxY) {
		EllipseClamp.ClampToEllipse(ref x, ref y, minX, maxX, minY, maxY);
		Assert.AreEqual(expectedX, x, Acc);
		Assert.AreEqual(expectedY, y, Acc);
    }

	private void AssertInside(float x, float y, float minX, float maxX, float minY, float maxY) {
		AssertClampedResult(x, y, x, y, minX, maxX, minY, maxY);
    }

	[TestMethod]
	public void TestInside() {
		AssertInside( 0,  0, -2, +3, -4, +5);
		AssertInside(-1,  0, -2, +3, -4, +5);
		AssertInside(+1,  0, -2, +3, -4, +5);
		AssertInside( 0, -1, -2, +3, -4, +5);
		AssertInside( 0, +1, -2, +3, -4, +5);
		AssertInside(-1, -1, -2, +3, -4, +5);
		AssertInside(-1, +1, -2, +3, -4, +5);
		AssertInside(+1, -1, -2, +3, -4, +5);
		AssertInside(+1, +1, -2, +3, -4, +5);
	}

	[TestMethod]
	public void TestOutsideOnAxis() {
		AssertClampedResult(-2, 0, -10, 0, -2, +3, -4, +5);
		AssertClampedResult(+3, 0, +10, 0, -2, +3, -4, +5);
		AssertClampedResult(0, -4, 0, -10, -2, +3, -4, +5);
		AssertClampedResult(0, +5, 0, +10, -2, +3, -4, +5);
	}

	[TestMethod]
	public void TestUnitCircle() {
		float sqrt2 = (float) Math.Sqrt(0.5);
		AssertClampedResult(sqrt2, sqrt2, +10, +10, -1, +1, -1, +1);
	}

	[TestMethod]
	public void TestOutside() {
		//expected values from Mathematica's NMinimize
		AssertClampedResult(-1.10904f, -3.32868f, -10, -10, -2, +3, -4, +5);
		AssertClampedResult(-1.05603f, +4.24618f, -10, +10, -2, +3, -4, +5);
		AssertClampedResult(+1.9619f, -3.02609f, +10, -10, -2, +3, -4, +5);
		AssertClampedResult(+1.8739f, +3.90459f, +10, +10, -2, +3, -4, +5);
	}

	[TestMethod]
	public void TestDegenerateToPoint() {
		//degenerate to point
		AssertClampedResult(0, 0, -10, -10, 0, 0, 0, 0);
		AssertClampedResult(0, 0, -10, +10, 0, 0, 0, 0);
		AssertClampedResult(0, 0, +10, -10, 0, 0, 0, 0);
		AssertClampedResult(0, 0, +10, +10, 0, 0, 0, 0);
	}

	[TestMethod]
	public void TestDegenerateToLineSegment() {
		//degenerate to line segment on x-axis
		AssertClampedResult(-2, 0, -10, -10, -2, +3, 0, 0);
		AssertClampedResult( 0, 0,   0, -10, -2, +3, 0, 0);
		AssertClampedResult(+3, 0, +10, -10, -2, +3, 0, 0);
		AssertClampedResult(-2, 0, -10, +10, -2, +3, 0, 0);
		AssertClampedResult( 0, 0,   0, +10, -2, +3, 0, 0);
		AssertClampedResult(+3, 0, +10, +10, -2, +3, 0, 0);

		//degenerate to line segment on y-axis
		AssertClampedResult(0, -4, -10, -10, 0, 0, -4, +5);
		AssertClampedResult(0,  0, -10,   0, 0, 0, -4, +5);
		AssertClampedResult(0, +5, -10, +10, 0, 0, -4, +5);
		AssertClampedResult(0, -4, +10, -10, 0, 0, -4, +5);
		AssertClampedResult(0,  0, +10,   0, 0, 0, -4, +5);
		AssertClampedResult(0, +5, +10, +10, 0, 0, -4, +5);
	}
}
