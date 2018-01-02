using MathNet.Numerics.LinearAlgebra;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace FlatIk {
	public class ExactHessianIkSolver : IIkSolver {
		private readonly List<Bone> bones;

		public ExactHessianIkSolver(List<Bone> bones) {
			this.bones = bones;
		}

		public void DoIteration(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 posedSource = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(inputs), unposedSource);
			List<Vector2> posedCenters = bones
				.Select(bone => Matrix3x2.TransformPoint(bone.GetChainedTransform(inputs), bone.Center))
				.ToList();
			
			List<Vector2> boneVectors = posedCenters
				.Zip(posedCenters.Skip(1).Concat(new [] {posedSource}), (center, end) => end - center)
				.ToList();

			List<float> weights = Enumerable.Range(0, bones.Count).Select(i => 1f / (bones.Count - i)).ToList();

			Vector<float> gradient = Vector<float>.Build.Dense(bones.Count);
			Matrix<float> hessian = Matrix<float>.Build.Dense(bones.Count, bones.Count);

			for (int i = 0; i < bones.Count; ++i) {
				Vector2 bi = boneVectors[i];

				Vector2 temp = target - posedSource + bi;

				gradient[i] = weights[i] * (bi.Y * temp.X - bi.X * temp.Y);
				hessian[i, i] = weights[i] * weights[i] * Vector2.Dot(bi, temp);

				for (int j = 0; j < bones.Count; ++j) {
					Vector2 bj = boneVectors[j];

					if (i != j) {
						hessian[i, j] = weights[i] * weights[j] * Vector2.Dot(bi, bj);
					}
				}
			}
			
			//var step = -hessian.Inverse().Multiply(gradient);
			var step = -hessian.PseudoInverse().Multiply(gradient);
			//var step = -0.5f * gradient;

			/*
			// Ensure step is approaching a minimum and not a maximum
			for (int i = 0; i < bones.Count; ++i) {
				step[i] = -Math.Abs(step[i]) * Math.Sign(gradient[i]);
			}
			*/
			
			for (int i = 0; i < bones.Count; ++i) {
				float localRotationDelta = step[i] - ((i > 0) ? step[i - 1] : 0);
				inputs.IncrementRotation(i, weights[i] * localRotationDelta);
			}
		}
	}
}
