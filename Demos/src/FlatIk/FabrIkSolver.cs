using System.Collections.Generic;
using SharpDX;

namespace FlatIk {
	public class FabrIkSolver : IIkSolver {
		private readonly List<Bone> bones;

		public FabrIkSolver(List<Bone> bones) {
			this.bones = bones;
		}
		
		private Vector2[] ExtractJointPositionsFromBones(Vector2 source) {
			Vector2[] jointPositions = new Vector2[bones.Count + 1];
			for (int i = 0; i < bones.Count; ++i) {
				var bone = bones[i];
				var transform = bone.GetChainedTransform();
				var center = Matrix3x2.TransformPoint(transform, bone.Center);
				jointPositions[i] = center;
			}
			jointPositions[bones.Count] = source;
			return jointPositions;
		}

		private void ApplyJointPositionsToBones(Vector2[] jointPositions) {
			float parentRotation = 0;

			for (int i = 0; i < bones.Count; ++i) {
				var bone = bones[i];
				
				float worldRotation = Vector2Utils.AngleBetween(
					bone.End - bone.Center,
					jointPositions[i + 1] - jointPositions[i]);
				float localRotation = worldRotation - parentRotation;

				bone.Rotation = localRotation;
				parentRotation = worldRotation;
			}
		}

		private void DoBackwardPass(Vector2[] jointPositions, Vector2 target) {
			for (int i = bones.Count - 1; i >= 0; --i) {
				Vector2 center = jointPositions[i];
				Vector2 end = jointPositions[i + 1];
				float length = Vector2.Distance(center, end);

				Vector2 newEnd = target;
				Vector2 newCenter = newEnd + length * Vector2.Normalize(center - newEnd);
				
				jointPositions[i + 1] = newEnd;
				target = newCenter;
			}
			jointPositions[0] = target;
		}

		private void DoForwardPass(Vector2[] jointPositions, Vector2 target) {
			for (int i = 0; i < bones.Count; ++i) {
				Vector2 center = jointPositions[i];
				Vector2 end = jointPositions[i + 1];
				float length = Vector2.Distance(center, end);

				Vector2 newCenter = target;
				Vector2 newEnd = newCenter + length * Vector2.Normalize(end - newCenter);
				
				jointPositions[i] = newCenter;
				target = newEnd;
			}
			jointPositions[bones.Count] = target;
		}

		public void DoIteration(Vector2 source, Vector2 target) {
			var jointPositions = ExtractJointPositionsFromBones(source);
			var root = jointPositions[0];

			DoBackwardPass(jointPositions, target);
			DoForwardPass(jointPositions, root);
			
			ApplyJointPositionsToBones(jointPositions);
		}
	}
}