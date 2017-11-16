using SharpDX;
using System.Collections.Generic;

namespace FlatIk {
	public class FabrIkChain {
		
		public static FabrIkChain Make(Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 posedSource = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(), sourceBone.End);

			List<Bone> bones = new List<Bone> { };
			List<Vector2> unposedPositions = new List<Vector2> { };
			List<Vector2> positions = new List<Vector2> { };

			unposedPositions.Add(unposedSource);
			positions.Add(posedSource);

			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				bones.Add(bone);
				
				var unposedCenter = bone.Center;
				unposedPositions.Add(unposedCenter);

				var posedCenter = Matrix3x2.TransformPoint(bone.GetChainedTransform(), unposedCenter);
				positions.Add(posedCenter);
			}

			var startTarget = target;
			var endTarget = positions[bones.Count];
			return new FabrIkChain(bones, unposedPositions, positions, target, endTarget);
		}
		
		private readonly List<Bone> bones;
		private readonly List<Vector2> unposedPositions;
		private readonly List<Vector2> positions;
		private readonly Vector2 startTarget; //target for end-effector
		private readonly Vector2 endTarget; //target for root

		private FabrIkChain(List<Bone> bones, List<Vector2> unposedPositions, List<Vector2> positions, Vector2 startTarget, Vector2 endTarget) {
			this.bones = bones;
			this.unposedPositions = unposedPositions;
			this.positions = positions;
			this.startTarget = startTarget;
			this.endTarget = endTarget;
		}

		//From end-effector to root
		public void DoForwardPass() {
			Vector2 target = startTarget;

			for (int i = 0; i < bones.Count; ++i) {
				Vector2 end = positions[i];
				Vector2 center = positions[i + 1];
				float length = Vector2.Distance(center, end);

				Vector2 newEnd = target;
				Vector2 newCenter = newEnd + length * Vector2.Normalize(center - newEnd);

				positions[i] = newEnd;
				target = newCenter;
			}

			positions[bones.Count] = target;
		}
		
		// From root to end-effector
		public void DoBackwardPass() {
			Vector2 target = endTarget;

			for (int i = bones.Count - 1; i >= 0; --i) {
				Vector2 end = positions[i];
				Vector2 center = positions[i + 1];
				float length = Vector2.Distance(center, end);
				
				Vector2 newCenter = target;
				Vector2 newEnd = newCenter + length * Vector2.Normalize(end - newCenter);
				
				positions[i + 1] = newCenter;
				target = newEnd;
			}

			positions[0] = target;
		}

		public void ApplyToBones() {
			float parentRotation = 0;

			for (int i = bones.Count - 1; i >= 0; --i) {
				var bone = bones[i];
				
				float worldRotation = Vector2Utils.AngleBetween(
					unposedPositions[i] - unposedPositions[i + 1],
					positions[i] - positions[i + 1]);
				float localRotation = worldRotation - parentRotation;

				bone.Rotation = localRotation;
				parentRotation = worldRotation;
			}
		}
	}

	public class FabrIkSolver : IIkSolver {
		public void DoIteration(Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			var chain = FabrIkChain.Make(sourceBone, unposedSource, target);

			chain.DoForwardPass();
			chain.DoBackwardPass();

			chain.ApplyToBones();
		}
	}
}