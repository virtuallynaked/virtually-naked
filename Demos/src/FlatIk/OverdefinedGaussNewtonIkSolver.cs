using System.Collections.Generic;
using SharpDX;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

namespace FlatIk {
	public class OverdefinedGaussNewtonIkSolver : IIkSolver {
		private const float BoneCenterWeight = 1f;
		private const float IkTargetWeight = 10f;

		private static IEnumerable<Bone> GetBoneChain(Bone sourceBone) {
			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				yield return bone;
			}
		}

		public void DoIteration(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 source = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(inputs), sourceBone.End);

			List<Bone> bones = GetBoneChain(sourceBone).ToList();
			int boneCount = bones.Count;
			
			Vector<float> residuals = Vector<float>.Build.Dense(boneCount * 2 + 2);
			for (int boneIdx = 0; boneIdx < boneCount; ++boneIdx) {
				var bone = bones[boneIdx];
				var posedCenter = Matrix3x2.TransformPoint(bone.GetChainedTransform(inputs), bone.Center);
				
				var residual = bone.Center - posedCenter;
				//var residual = Vector2.Zero;
				residuals[boneIdx * 2 + 0] = BoneCenterWeight * residual.X;
				residuals[boneIdx * 2 + 1] = BoneCenterWeight * residual.Y;
			}
			residuals[boneCount * 2 + 0] = IkTargetWeight * (target.X - source.X);
			residuals[boneCount * 2 + 1] = IkTargetWeight * (target.Y - source.Y);

			Matrix<float> jacobian = Matrix<float>.Build.Dense(2 * boneCount + 2, boneCount);
			
			for (int boneIdx = 0; boneIdx < boneCount; ++boneIdx) {
				for (int targetIdx = 0; targetIdx < boneCount + 1; ++targetIdx) {
					Vector2 boneGradient;
					if (targetIdx > boneIdx && targetIdx != boneCount) {
						//target is unaffected by this bone
						boneGradient = Vector2.Zero;
					} else {
						Vector2 targetSource;
						float weight;

						if (targetIdx < boneCount) {
							//target is a bone center
							var targetBone = bones[targetIdx];
							targetSource = Matrix3x2.TransformPoint(targetBone.GetChainedTransform(inputs), targetBone.Center);
							weight = BoneCenterWeight;

						} else {
							//target is IK target
							targetSource = source;
							weight = IkTargetWeight;
						}
						boneGradient = weight * bones[boneIdx].GetGradientOfTransformedPointWithRespectToRotation(inputs, targetSource);
					} 
					
					jacobian[targetIdx * 2 + 0, boneIdx] = boneGradient.X;
					jacobian[targetIdx * 2 + 1, boneIdx] = boneGradient.Y;
				}
			}
			Vector<float> step = jacobian.PseudoInverse().Multiply(residuals);
			
			for (int boneIdx = 0; boneIdx < boneCount; ++boneIdx) {
				var bone = bones[boneIdx];
				bone.IncrementRotation(inputs, step[boneIdx]);
			}
		}
	}
}
