using System.Collections.Generic;
using SharpDX;

namespace FlatIk {
	public class SimpleIkSolver : IIkSolver {
		private readonly List<Bone> bones;

		public SimpleIkSolver(List<Bone> bones) {
			this.bones = bones;
		}
		
		private void AdjustBone(Bone bone, Vector2 source, Vector2 target, float weight) {
			var transform = bone.GetChainedTransform();
			var center = Matrix3x2.TransformPoint(transform, bone.Center);
			
			float rotationDelta = Vector2Utils.AngleBetween(source - center, target - center);

			bone.Rotation += rotationDelta * weight;
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