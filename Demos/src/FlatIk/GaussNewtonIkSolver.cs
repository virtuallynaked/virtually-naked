using System.Collections.Generic;
using SharpDX;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

namespace FlatIk {
	public class GaussNewtonIkSolver : IIkSolver {
		private static IEnumerable<Bone> GetBoneChain(Bone sourceBone) {
			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				yield return bone;
			}
		}

		public void DoIteration(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 source = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(inputs), sourceBone.End);
			Vector<float> residuals = Vector<float>.Build.Dense(2);
			residuals[0] = target.X - source.X;
			residuals[1] = target.Y - source.Y;

			List<Bone> bones = GetBoneChain(sourceBone).ToList();
			
			Matrix<float> jacobian = Matrix<float>.Build.Dense(2, bones.Count);
			
			for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
				Vector2 boneGradient = bones[boneIdx].GetGradientOfTransformedPointWithRespectToRotation(inputs, source);
				jacobian[0, boneIdx] = boneGradient.X;
				jacobian[1, boneIdx] = boneGradient.Y;
			}
			
			Vector<float> step = jacobian.PseudoInverse().Multiply(residuals);
			
			for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
				var bone = bones[boneIdx];
				bone.IncrementRotation(inputs, step[boneIdx]);
			}
		}
	}
}