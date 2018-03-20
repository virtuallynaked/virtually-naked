using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class RigidTransformTest {
	public float Acc = 1e-4f;

	[TestMethod]
	public void TestFromAndToRotationTranslation() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);

		RigidTransform transform = RigidTransform.FromRotationTranslation(rotation, translation);

		Assert.AreEqual(0, Vector3.Distance(translation, transform.Translation), 1e-6);
		Assert.IsTrue(transform.Rotation == rotation || transform.Rotation == -rotation);
	}

	[TestMethod]
	public void TestFromMatrix() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);
		Matrix matrix = Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		RigidTransform transform = RigidTransform.FromMatrix(matrix);

		Assert.AreEqual(0, Vector3.Distance(translation, transform.Translation), 1e-6);
		Assert.IsTrue(transform.Rotation == rotation || transform.Rotation == -rotation);
	}

	[TestMethod]
	public void TestFromMatrixRobustness() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);
		Vector3 scale = new Vector3(3,4,5);
		Matrix matrix = Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		RigidTransform transform = RigidTransform.FromMatrix(matrix);

		Assert.AreEqual(0, Vector3.Distance(translation, transform.Translation), 1e-6);
		Assert.IsTrue(transform.Rotation == rotation || transform.Rotation == -rotation);
	}

	[TestMethod]
	public void TestTransform() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Matrix matrix = Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		RigidTransform transform = RigidTransform.FromRotationTranslation(rotation, translation);

		Vector3 v = new Vector3(3,4,5);
		Assert.IsTrue(transform.Transform(v) == Vector3.TransformCoordinate(v, matrix));
	}

	[TestMethod]
	public void TestTransformAndInvert() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		RigidTransform transform = RigidTransform.FromRotationTranslation(rotation, translation);
		
		Vector3 v = new Vector3(3,4,5);

		Vector3 transformed = transform.Transform(v);
		Vector3 inverseTransformed = transform.InverseTransform(transformed);

		Assert.AreEqual(
			v,
			inverseTransformed);
	}

	[TestMethod]
	public void TestInvert() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		RigidTransform transform = RigidTransform.FromRotationTranslation(rotation, translation);

		Vector3 v = new Vector3(3,4,5);

		Vector3 transformed = transform.Transform(v);
		Vector3 inverseTransformed = transform.Invert().Transform(transformed);

		Assert.AreEqual(
			v,
			inverseTransformed);
	}

	[TestMethod]
	public void TestChain() {
		var rnd = new Random(0);
		RigidTransform transform1 = RandomUtil.RigidTransform(rnd);
		RigidTransform transform2 = RandomUtil.RigidTransform(rnd);
		var testPoint = RandomUtil.Vector3(rnd);

		MathAssert.AreEqual(
			transform2.Transform(transform1.Transform(testPoint)),
			transform1.Chain(transform2).Transform(testPoint),
			Acc);
	}

	[TestMethod]
	public void TestFromRotationAboutCenter() {
		var rnd = new Random(0);
		var rotation = RandomUtil.UnitQuaternion(rnd);
		var center = RandomUtil.Vector3(rnd);

		RigidTransform transform = RigidTransform.FromRotation(rotation, center);

		var testPoint = RandomUtil.Vector3(rnd);

		MathAssert.AreEqual(
			Vector3.Transform(testPoint - center, rotation) + center,
			transform.Transform(testPoint),
			Acc);
	}
}
