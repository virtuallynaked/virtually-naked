using System.Collections.Generic;
using SharpDX;
using MathNet.Numerics.LinearAlgebra;

namespace FlatIk {
	public class GaussNewtonIkSolver : IIkSolver {
		private readonly List<Bone> bones;

		public GaussNewtonIkSolver(List<Bone> bones) {
			this.bones = bones;
		}
		
		public void DoIteration(Vector2 source, Vector2 target) {
			Vector<float> residuals = Vector<float>.Build.Dense(2);
			residuals[0] = target.X - source.X;
			residuals[1] = target.Y - source.Y;

			Matrix<float> jacobian = Matrix<float>.Build.Dense(2, bones.Count);
			
			for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
				Vector2 boneGradient = bones[boneIdx].GetGradientOfTransformedPointWithRespectToRotation(source);
				jacobian[0, boneIdx] = boneGradient.X;
				jacobian[1, boneIdx] = boneGradient.Y;
			}
			
			Vector<float> step = jacobian.PseudoInverse().Multiply(residuals);
			
			for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
				bones[boneIdx].Rotation += step[boneIdx];
			}
		}
	}
}