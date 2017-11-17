using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

namespace FlatIk {
	[TestClass]
    public class BoneTest  {
		private readonly Bone bone0;
		private readonly Bone bone1;
		private readonly Bone bone2;
		private readonly SkeletonInputs inputs;

		public BoneTest() {
			bone0 = Bone.MakeWithOffset(0, null, Vector2.UnitX);
			bone1 = Bone.MakeWithOffset(1, bone0, Vector2.UnitX);
			bone2 = Bone.MakeWithOffset(2, bone1, Vector2.UnitX);

			inputs = new SkeletonInputs(3);
			bone0.SetRotation(inputs, +0.1f);
			bone1.SetRotation(inputs, -0.2f);
			bone2.SetRotation(inputs, +0.4f);
		}
		
		[TestMethod]
		public void TestRetransformPoint() {
			var point = new Vector2(2, 3);
			
			var transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(inputs), point);

			float rotationDelta = 0.3f;

			var retransformedPoint = bone1.RetransformPoint(inputs, rotationDelta, transformedPoint);

			bone1.IncrementRotation(inputs, rotationDelta);
			var expectedRetransformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(inputs), point);

			Assert.AreEqual(expectedRetransformedPoint, retransformedPoint);
		}

		[TestMethod]
		public void TestRetransformPointByZero() {
			var point = new Vector2(2, 3);
			
			var transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(inputs), point);
			var retransformedPoint = bone1.RetransformPoint(inputs, 0, transformedPoint);
			Assert.AreEqual(transformedPoint, retransformedPoint);
		}

		[TestMethod]
		public void TestGradientOfTransformedPointWithRespectToRotation() {
			Vector2 point = new Vector2(2, 3);

			Vector2 transformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(inputs), point);

			Vector2 gradient = bone1.GetGradientOfTransformedPointWithRespectToRotation(inputs, transformedPoint);

			float rotationStepSize = 1e-3f;
			bone1.IncrementRotation(inputs, rotationStepSize);
			Vector2 steppedTransformedPoint = Matrix3x2.TransformPoint(bone2.GetChainedTransform(inputs), point);

			Vector2 finiteDifferenceApproximationToGradient = (steppedTransformedPoint - transformedPoint) / rotationStepSize;
			
			Assert.AreEqual(finiteDifferenceApproximationToGradient.X, gradient.X, 1e-3);
			Assert.AreEqual(finiteDifferenceApproximationToGradient.Y, gradient.Y, 1e-3);
		}

		[TestMethod]
		public void TestSetRotation() {
			inputs.SetRotation(0, +0.01f);
			Assert.AreEqual(+0.01f, inputs.GetRotation(0), 1e-6);

			inputs.SetRotation(0, -0.01f);
			Assert.AreEqual(-0.01f, inputs.GetRotation(0), 1e-6);
			
			inputs.SetRotation(0, MathUtil.TwoPi);
			Assert.AreEqual(0, inputs.GetRotation(0), 1e-6);
			
			inputs.SetRotation(0, MathUtil.Pi - 0.01f);
			Assert.AreEqual(MathUtil.Pi - 0.01f, inputs.GetRotation(0), 1e-6);
			
			inputs.SetRotation(0, MathUtil.Pi + 0.01f);
			Assert.AreEqual(-MathUtil.Pi + 0.01f, inputs.GetRotation(0), 1e-6);

			inputs.SetRotation(0, -MathUtil.Pi + 0.01f);
			Assert.AreEqual(-MathUtil.Pi + 0.01f, inputs.GetRotation(0), 1e-6);
			
			inputs.SetRotation(0, -MathUtil.Pi - 0.01f);
			Assert.AreEqual(MathUtil.Pi - 0.01f, inputs.GetRotation(0), 1e-6);
		}
    }
}
