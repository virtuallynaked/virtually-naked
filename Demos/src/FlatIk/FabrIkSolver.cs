using SharpDX;
using System;
using System.Collections.Generic;

namespace FlatIk {
	public class FabrIkChain {
		
		public static FabrIkChain Make(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 posedSource = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(inputs), sourceBone.End);

			List<Bone> bones = new List<Bone> { };
			List<Vector2> unposedPositions = new List<Vector2> { };
			List<Vector2> positions = new List<Vector2> { };

			unposedPositions.Add(unposedSource);
			positions.Add(posedSource);
			
			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				bones.Add(bone);
				
				var unposedCenter = bone.Center;
				unposedPositions.Add(unposedCenter);

				var posedCenter = Matrix3x2.TransformPoint(bone.GetChainedTransform(inputs), unposedCenter);
				positions.Add(posedCenter);
			}

			List<float> rotations = new List<float>();
			for (int i = 0; i < bones.Count; ++i) {
				float rotation = Vector2Utils.AngleBetween(
					unposedPositions[i] - unposedPositions[i + 1],
					positions[i] - positions[i + 1]);
				rotations.Add(rotation);
			}

			var startTarget = target;
			var endTarget = positions[bones.Count];
			return new FabrIkChain(bones, unposedPositions, rotations, positions, target, endTarget);
		}
		
		private readonly List<Bone> bones;

		private readonly List<Vector2> unposedPositions;

		private readonly List<float> rotations;
		private readonly List<Vector2> positions;

		private readonly Vector2 startTarget; //target for end-effector
		private readonly Vector2 endTarget; //target for root

		private FabrIkChain(List<Bone> bones, List<Vector2> unposedPositions, List<float> rotations, List<Vector2> positions, Vector2 startTarget, Vector2 endTarget) {
			this.bones = bones;
			this.unposedPositions = unposedPositions;

			this.rotations = rotations;
			this.positions = positions;

			this.startTarget = startTarget;
			this.endTarget = endTarget;
		}
		
		public float ConstrainForwardRotation(int boneIdx, float desiredRotation) {
			if (boneIdx == 0) {
				return desiredRotation;
			} else {
				float childRotation = rotations[boneIdx - 1];
				float desiredChildLocalRotation = (float) Math.IEEERemainder(childRotation - desiredRotation, Math.PI * 2);
				float limit = bones[boneIdx - 1].RotationLimit;
				float childLocalRotation = MathUtil.Clamp(desiredChildLocalRotation, -limit, +limit);
				return childRotation - childLocalRotation;
			}
		}

		public float ConstrainBackwardRotation(int boneIdx, float desiredRotation) {
			float parentRotation = (boneIdx + 1 < bones.Count) ? rotations[boneIdx + 1] : 0;
			float desiredLocalRotation = (float) Math.IEEERemainder(desiredRotation - parentRotation, Math.PI * 2);
			float limit = bones[boneIdx].RotationLimit;
			float localRotation = MathUtil.Clamp(desiredLocalRotation, -limit, +limit);
			return parentRotation + localRotation;
		}

		//From end-effector to root
		public void DoForwardPass() {
			Vector2 target = startTarget;

			for (int i = 0; i < bones.Count; ++i) {
				Vector2 end = positions[i];
				Vector2 center = positions[i + 1];
				float length = Vector2.Distance(center, end);

				Vector2 newEnd = target;

				Vector2 desiredCenter = newEnd + length * Vector2.Normalize(center - newEnd);
				float desiredRotation = Vector2Utils.AngleBetween(
					unposedPositions[i] - unposedPositions[i + 1],
					newEnd - desiredCenter);
				float newRotation = ConstrainForwardRotation(i, desiredRotation);
				Vector2 newCenter = newEnd - Vector2Utils.RotateBy(newRotation, unposedPositions[i] - unposedPositions[i + 1]);
				
				rotations[i] = newRotation;
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

				Vector2 desiredEnd = newCenter + length * Vector2.Normalize(end - newCenter);
				float desiredRotation = Vector2Utils.AngleBetween(
					unposedPositions[i] - unposedPositions[i + 1],
					desiredEnd - newCenter);
				float newRotation = ConstrainBackwardRotation(i, desiredRotation);
				Vector2 newEnd = newCenter + Vector2Utils.RotateBy(newRotation, unposedPositions[i] - unposedPositions[i + 1]);
				
				rotations[i] = newRotation;
				target = newEnd;
				positions[i + 1] = newCenter;
			}

			positions[0] = target;
		}

		public void ApplyToInputs(SkeletonInputs inputs) {
			inputs.Translation = positions[bones.Count] - unposedPositions[bones.Count];

			for (int i = 0; i < bones.Count; ++i) {
				float parentRotation = (i + 1 < bones.Count) ? rotations[i + 1] : 0;
				float localRotation = rotations[i] - parentRotation;

				bones[i].SetRotation(inputs, localRotation);
			}
		}
	}

	public class FabrIkSolver : IIkSolver {
		public void DoIteration(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			var chain = FabrIkChain.Make(inputs, sourceBone, unposedSource, target);

			chain.DoForwardPass();
			chain.DoBackwardPass();

			chain.ApplyToInputs(inputs);
		}
	}
}