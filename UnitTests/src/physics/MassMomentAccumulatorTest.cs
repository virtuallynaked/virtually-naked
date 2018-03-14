using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class MassMomentAccumulatorTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestCenterOfMass() {
		Random rnd = new Random(0);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		var accum = new MassMomentAccumulator();
		accum.Add(m1, p1);
		accum.Add(m2, p2);
		accum.Add(m3, p3);
		
		Vector3 expectedCenterOfMass = (m1 * p1 + m2 * p2 + m3 * p3) / (m1 + m2 + m3);
		MathAssert.AreEqual(expectedCenterOfMass, accum.GetCenterOfMass(), Acc);
	}

	private static float PointMassMomentOfInertia(float mass, Vector3 position, Vector3 axisOfRotation, Vector3 centerOfRotation) {
		Vector3 relativePosition = position - centerOfRotation;
		Vector3 closestPointOnAxis = axisOfRotation * Vector3.Dot(relativePosition, axisOfRotation);
		float momentOfInertia = mass * (relativePosition - closestPointOnAxis).LengthSquared();
		return momentOfInertia;
	}

	[TestMethod]
	public void TestMomentOfInertia() {
		Random rnd = new Random(1);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		var accum = new MassMomentAccumulator();
		accum.Add(m1, p1);
		accum.Add(m2, p2);
		accum.Add(m3, p3);

		Vector3 axisOfRotation = RandomUtil.UnitVector3(rnd);
		Vector3 centerOfRotation = RandomUtil.Vector3(rnd);
		
		float expectedMomentOfInertia =
			PointMassMomentOfInertia(m1, p1, axisOfRotation, centerOfRotation) +
			PointMassMomentOfInertia(m2, p2, axisOfRotation, centerOfRotation) +
			PointMassMomentOfInertia(m3, p3, axisOfRotation, centerOfRotation);
		Assert.AreEqual(expectedMomentOfInertia, accum.GetMomentOfInertia(axisOfRotation, centerOfRotation), Acc);
	}

	[TestMethod]
	public void TestAddingAccumulators() {
		Random rnd = new Random(2);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		var accumA = new MassMomentAccumulator();
		accumA.Add(m1, p1);
		accumA.Add(m2, p2);
		accumA.Add(m3, p3);

		var accumB = new MassMomentAccumulator();
		accumB.Add(m1, p1);
		var accumB2 = new MassMomentAccumulator();
		accumB2.Add(m2, p2);
		accumB2.Add(m3, p3);
		accumB.Add(accumB2);
		
		MathAssert.AreEqual(accumA.GetCenterOfMass(), accumB.GetCenterOfMass(), Acc);
	}
}