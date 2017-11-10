using System.Collections.Generic;
using SharpDX;
using System;

namespace FlatIk {
	public class SimpleIkSolver : IIkSolver {
		private readonly List<Bone> bones;

		public SimpleIkSolver(List<Bone> bones) {
			this.bones = bones;
		}
		
		private float Atan(Vector2 v) {
			return (float) Math.Atan2(v.Y, v.X);
		}

		private float AngleDelta(Vector2 from, Vector2 to) {
			float angleDelta = Atan(to) - Atan(from);

			while (angleDelta < -MathUtil.Pi) {
				angleDelta += MathUtil.TwoPi;
			}
			while (angleDelta > +MathUtil.Pi) {
				angleDelta -= MathUtil.TwoPi;
			}
			
			return angleDelta;
		}

		private void AdjustBone(Bone bone, Vector2 source, Vector2 target, float weight) {
			var transform = bone.GetChainedTransform();
			var center = Matrix3x2.TransformPoint(transform, bone.Center);
			
			float angleDelta = AngleDelta(source - center, target - center);

			bone.Rotation += angleDelta * weight;
		}

		public void DoIteration(Vector2 source, Vector2 target) {
			var sourceBone = bones[bones.Count - 1];

			float decay = 0.9f;
			float weight = 1 - decay;
			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				AdjustBone(bone, source, target, weight);
				weight *= decay;
			}
		}
	}
}