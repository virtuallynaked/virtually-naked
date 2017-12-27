using System.Collections.Generic;
using SharpDX;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

namespace FlatIk {
	public class GaussNewtonIkSolver : IIkSolver {
		private const float RootTranslationWeight = 1f;

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
			int boneCount = bones.Count;
			
			Matrix<float> jacobian = Matrix<float>.Build.Dense(2, boneCount + 2);
			
			for (int boneIdx = 0; boneIdx < boneCount; ++boneIdx) {
				Vector2 boneGradient = bones[boneIdx].GetGradientOfTransformedPointWithRespectToRotation(inputs, source);
				jacobian[0, boneIdx] = boneGradient.X;
				jacobian[1, boneIdx] = boneGradient.Y;
			}
			jacobian[0, boneCount + 0] = RootTranslationWeight;
			jacobian[1, boneCount + 1] = RootTranslationWeight;
			
			Vector<float> step = jacobian.PseudoInverse().Multiply(residuals);
			
			for (int boneIdx = 0; boneIdx < boneCount; ++boneIdx) {
				var bone = bones[boneIdx];
				bone.IncrementRotation(inputs, step[boneIdx]);
			}

			Vector2 rootTranslationStep = new Vector2(
				step[boneCount + 0],
				step[boneCount + 1]);
			inputs.Translation += rootTranslationStep * RootTranslationWeight;
		}
	}
}