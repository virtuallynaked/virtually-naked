using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

namespace FlatIk {
	[TestClass]
    public class BoneTest  {
		private readonly Bone bone0;
		private readonly Bone bone1;
		private readonly Bone bone2;

		public BoneTest() {
			bone0 = Bone.MakeWithOffset(null, Vector2.UnitX, 0.1f);
			bone1 = Bone.MakeWithOffset(bone0, Vector2.UnitX, -0.2f);
			bone2 = Bone.MakeWithOffset(bone1, Vector2.UnitX, 0.4f);
		}
		
		[TestMethod]
		public void TestRetransformPoint() {
			var point = new Vector2(2, 3);
			
			var transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(), point);

			float rotationDelta = 0.3f;

			var retransformedPoint = bone1.RetransformPoint(rotationDelta, transformedPoint);

			bone1.Rotation += rotationDelta;
			var expectedRetransformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(), point);

			Assert.AreEqual(expectedRetransformedPoint, retransformedPoint);
		}

		[TestMethod]
		public void TestRetransformPointByZero() {
			var point = new Vector2(2, 3);
			
			var transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(), point);
			var retransformedPoint = bone1.RetransformPoint(0, transformedPoint);
			Assert.AreEqual(transformedPoint, retransformedPoint);
		}

		[TestMethod]
		public void TestGradientOfTransformedPointWithRespectToRotation() {
			Vector2 point = new Vector2(2, 3);

			Vector2 transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(), point);

			Vector2 gradient = bone1.GetGradientOfTransformedPointWithRespectToRotation(transformedPoint);

			float rotationStepSize = 1e-3f;
			bone1.Rotation += rotationStepSize;
			Vector2 steppedTransformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(), point);

			Vector2 finiteDifferenceApproximationToGradient = (steppedTransformedPoint - transformedPoint) / rotationStepSize;
			
			Assert.AreEqual(finiteDifferenceApproximationToGradient.X, gradient.X, 1e-3);
			Assert.AreEqual(finiteDifferenceApproximationToGradient.Y, gradient.Y, 1e-3);
		}

		[TestMethod]
		public void TestSetRotation() {
			bone0.Rotation = +0.01f;
			Assert.AreEqual(+0.01f, bone0.Rotation, 1e-6);

			bone0.Rotation = -0.01f;
			Assert.AreEqual(-0.01f, bone0.Rotation, 1e-6);

			bone0.Rotation = MathUtil.TwoPi;
			Assert.AreEqual(0, bone0.Rotation, 1e-6);

			bone0.Rotation = MathUtil.Pi - 0.01f;
			Assert.AreEqual(MathUtil.Pi - 0.01f, bone0.Rotation, 1e-6);
			
			bone0.Rotation = MathUtil.Pi + 0.01f;
			Assert.AreEqual(-MathUtil.Pi + 0.01f, bone0.Rotation, 1e-6);

			bone0.Rotation = -MathUtil.Pi + 0.01f;
			Assert.AreEqual(-MathUtil.Pi + 0.01f, bone0.Rotation, 1e-6);
			
			bone0.Rotation = -MathUtil.Pi - 0.01f;
			Assert.AreEqual(MathUtil.Pi - 0.01f, bone0.Rotation, 1e-6);
		}
    }
}
