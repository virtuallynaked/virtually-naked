using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class MassMomentTest {
	private const float Acc = 1e-4f;

	[TestMethod]
	public void TestCenterOfMass() {
		Random rnd = new Random(0);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		var accum = new MassMoment();
		accum.AddInplace(m1, p1);
		accum.AddInplace(m2, p2);
		accum.AddInplace(m3, p3);
		
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

		var accum = new MassMoment();
		accum.AddInplace(m1, p1);
		accum.AddInplace(m2, p2);
		accum.AddInplace(m3, p3);

		Vector3 axisOfRotation = RandomUtil.UnitVector3(rnd);
		Vector3 centerOfRotation = RandomUtil.Vector3(rnd);
		
		float expectedMomentOfInertia =
			PointMassMomentOfInertia(m1, p1, axisOfRotation, centerOfRotation) +
			PointMassMomentOfInertia(m2, p2, axisOfRotation, centerOfRotation) +
			PointMassMomentOfInertia(m3, p3, axisOfRotation, centerOfRotation);
		Assert.AreEqual(expectedMomentOfInertia, accum.GetMomentOfInertia(axisOfRotation, centerOfRotation), Acc);
	}

	[TestMethod]
	public void TestAddingMassMoments() {
		Random rnd = new Random(2);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		var accumA = new MassMoment();
		accumA.AddInplace(m1, p1);
		accumA.AddInplace(m2, p2);
		accumA.AddInplace(m3, p3);

		var accumB = new MassMoment();
		accumB.AddInplace(m1, p1);
		var accumB2 = new MassMoment();
		accumB2.AddInplace(m2, p2);
		accumB2.AddInplace(m3, p3);
		accumB.AddInplace(accumB2);
		
		PhysicsAssert.AreEqual(accumA, accumB, Acc);
	}

	[TestMethod]
	public void TestAddFlexibleInPlace() {
		Random rnd = new Random(2);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		MassMoment momentToAdd = new MassMoment();
		momentToAdd.AddInplace(m1, p1);
		momentToAdd.AddInplace(m2, p2);
		momentToAdd.AddInplace(m3, p3);

		float flexiblity = 0.6f;
		Vector3 centerOfRotation = RandomUtil.Vector3(rnd);
		MassMoment massMoment = new MassMoment();
		massMoment.AddFlexibleInPlace(momentToAdd, flexiblity, centerOfRotation);

		MassMoment expectedMoment = new MassMoment();
		expectedMoment.AddInplace((1 - flexiblity) * m1, p1);
		expectedMoment.AddInplace((1 - flexiblity) * m2, p2);
		expectedMoment.AddInplace((1 - flexiblity) * m3, p3);
		expectedMoment.AddInplace(flexiblity * m1, centerOfRotation);
		expectedMoment.AddInplace(flexiblity * m2, centerOfRotation);
		expectedMoment.AddInplace(flexiblity * m3, centerOfRotation);

		PhysicsAssert.AreEqual(massMoment, expectedMoment, Acc);
	}
	
	[TestMethod]
	public void TestTranslate() {
		Random rnd = new Random(1);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		Vector3 translation = RandomUtil.Vector3(rnd);

		var massMoment = new MassMoment();
		var translatedMassMoment = new MassMoment();
		massMoment.AddInplace(m1, p1); translatedMassMoment.AddInplace(m1, p1 + translation);
		massMoment.AddInplace(m2, p2); translatedMassMoment.AddInplace(m2, p2 + translation);
		massMoment.AddInplace(m3, p3); translatedMassMoment.AddInplace(m3, p3 + translation);

		PhysicsAssert.AreEqual(massMoment.Translate(translation), translatedMassMoment, Acc);
	}

	[TestMethod]
	public void TestRotate() {
		Random rnd = new Random(1);
		Vector3 p1 = RandomUtil.Vector3(rnd); float m1 = RandomUtil.PositiveFloat(rnd);
		Vector3 p2 = RandomUtil.Vector3(rnd); float m2 = RandomUtil.PositiveFloat(rnd);
		Vector3 p3 = RandomUtil.Vector3(rnd); float m3 = RandomUtil.PositiveFloat(rnd);

		Quaternion rotation = RandomUtil.UnitQuaternion(rnd);

		var massMoment = new MassMoment();
		var rotatedMassMoment = new MassMoment();
		massMoment.AddInplace(m1, p1); rotatedMassMoment.AddInplace(m1, Vector3.Transform(p1, rotation));
		massMoment.AddInplace(m2, p2); rotatedMassMoment.AddInplace(m2, Vector3.Transform(p2, rotation));
		massMoment.AddInplace(m3, p3); rotatedMassMoment.AddInplace(m3, Vector3.Transform(p3, rotation));

		PhysicsAssert.AreEqual(massMoment.Rotate(rotation), rotatedMassMoment, Acc);
	}
}
