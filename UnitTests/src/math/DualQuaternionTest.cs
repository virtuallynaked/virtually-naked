using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class DualQuaternionTest {
	[TestMethod]
	public void TestFromAndToRotationTranslation() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);

		DualQuaternion dq = DualQuaternion.FromRotationTranslation(rotation, translation);

		Assert.AreEqual(0, Vector3.Distance(translation, dq.Translation), 1e-6);
		Assert.IsTrue(dq.Rotation == rotation || dq.Rotation == -rotation);
	}

	[TestMethod]
	public void TestFromMatrix() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);
		Matrix matrix = Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		DualQuaternion dq = DualQuaternion.FromMatrix(matrix);

		Assert.AreEqual(0, Vector3.Distance(translation, dq.Translation), 1e-6);
		Assert.IsTrue(dq.Rotation == rotation || dq.Rotation == -rotation);
	}

	[TestMethod]
	public void TestFromMatrixRobustness() {
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Vector3 translation = new Vector3(2,3,4);
		Vector3 scale = new Vector3(3,4,5);
		Matrix matrix = Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		DualQuaternion dq = DualQuaternion.FromMatrix(matrix);

		Assert.AreEqual(0, Vector3.Distance(translation, dq.Translation), 1e-6);
		Assert.IsTrue(dq.Rotation == rotation || dq.Rotation == -rotation);
	}

	[TestMethod]
	public void TestTransform() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		Matrix matrix = Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

		DualQuaternion dq = DualQuaternion.FromRotationTranslation(rotation, translation);

		Vector3 v = new Vector3(3,4,5);
		Assert.IsTrue(dq.Transform(v) == Vector3.TransformCoordinate(v, matrix));
	}

	[TestMethod]
	public void TestTransformAndInvert() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		DualQuaternion dq = DualQuaternion.FromRotationTranslation(rotation, translation);
		
		Vector3 v = new Vector3(3,4,5);

		Vector3 transformed = dq.Transform(v);
		Vector3 inverseTransformed = dq.InverseTransform(transformed);

		Assert.AreEqual(
			v,
			inverseTransformed);
	}

	[TestMethod]
	public void TestInvert() {
		Vector3 translation = new Vector3(2,3,4);
		Quaternion rotation = Quaternion.RotationYawPitchRoll(1,2,3);
		DualQuaternion dq = DualQuaternion.FromRotationTranslation(rotation, translation);

		Vector3 v = new Vector3(3,4,5);

		Vector3 transformed = dq.Transform(v);
		Vector3 inverseTransformed = dq.Invert().Transform(transformed);

		Assert.AreEqual(
			v,
			inverseTransformed);
	}
}
