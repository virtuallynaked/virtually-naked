using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class ScalingTransformTest {
	[TestMethod]
	public void TestConstructor() {
		Matrix3x3 scaling = Matrix3x3.Scaling(1,2,3);
		Vector3 translation = new Vector3(4,5,6);
		ScalingTransform transform = new ScalingTransform(scaling, translation);
		Matrix expectedEquivalentTransform = new Matrix(
			scaling.M11, scaling.M12, scaling.M13, 0,
			scaling.M21, scaling.M22, scaling.M23, 0,
			scaling.M31, scaling.M32, scaling.M33, 0,
			0, 0, 0, 1) * Matrix.Translation(translation);

		Vector3 testPoint = new Vector3(7,8,9);

		Assert.AreEqual(
			transform.Transform(testPoint),
			Vector3.TransformCoordinate(testPoint, expectedEquivalentTransform));
	}

	[TestMethod]
	public void TestTransformAndInvert() {
		ScalingTransform transform = new ScalingTransform(Matrix3x3.Scaling(1,2,3), new Vector3(2,3,4));
		Vector3 v = new Vector3(3,4,5);

		Vector3 transformed = transform.Transform(v);
		Vector3 inverseTransformed = transform.InverseTransform(transformed);

		Assert.AreEqual(
			v,
			inverseTransformed);
	}

	[TestMethod]
	public void TestChain() {
		ScalingTransform transform1 = new ScalingTransform(Matrix3x3.Scaling(1,2,3), new Vector3(2,3,4));
		ScalingTransform transform2 = new ScalingTransform(Matrix3x3.Scaling(3,4,5), new Vector3(4,5,6));
		
		Vector3 testPoint = new Vector3(7,8,9);

		Assert.AreEqual(
			transform2.Transform(transform1.Transform(testPoint)),
			transform1.Chain(transform2).Transform(testPoint));
	}
}
