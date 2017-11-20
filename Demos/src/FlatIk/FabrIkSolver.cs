using SharpDX;
using System;
using System.Collections.Generic;

namespace FlatIk {
	public class FabrIkChain {
		
		public static FabrIkChain Make(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target) {
			Vector2 posedSource = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(inputs), sourceBone.End);

			List<Bone> bones = new List<Bone> { };
			List<Vector2> unposedBoneVectors = new List<Vector2> { };
			
			List<float> rotations = new List<float>();

			List<Vector2> positions = new List<Vector2> { };
			positions.Add(posedSource);

			Vector2 previousUnposedPosition = unposedSource;
			Vector2 previousPosedPosition = posedSource;
			for (var bone = sourceBone; bone != null; bone = bone.Parent) {
				var unposedCenter = bone.Center;
				var posedCenter = Matrix3x2.TransformPoint(bone.GetChainedTransform(inputs), unposedCenter);
				
				Vector2 unposedBoneVector = previousUnposedPosition - unposedCenter;
				Vector2 posedBoneVector = previousPosedPosition - posedCenter;
				float rotation = Vector2Utils.AngleBetween(
					unposedBoneVector,
					posedBoneVector);
				
				bones.Add(bone);
				unposedBoneVectors.Add(unposedBoneVector);
				rotations.Add(rotation);
				positions.Add(posedCenter);
				
				previousUnposedPosition = unposedCenter;
				previousPosedPosition = posedCenter;
			}
			
			var startTarget = target;
			var endTarget = positions[bones.Count];
			return new FabrIkChain(bones, unposedBoneVectors, rotations, positions, target, endTarget);
		}
		
		private readonly List<Bone> bones;

		private readonly List<Vector2> unposedBoneVectors;

		private readonly List<float> rotations;
		private readonly List<Vector2> positions;

		private readonly Vector2 firstTargetEnd; //target for end-effector
		private readonly Vector2 lastTargetCenter; //target for root

		private FabrIkChain(List<Bone> bones, List<Vector2> unposedBoneVectors, List<float> rotations, List<Vector2> positions, Vector2 startTarget, Vector2 endTarget) {
			this.bones = bones;
			this.unposedBoneVectors = unposedBoneVectors;

			this.rotations = rotations;
			this.positions = positions;

			this.firstTargetEnd = startTarget;
			this.lastTargetCenter = endTarget;
		}
		
		public float ConstrainRotationAgainstChild(int boneIdx, float desiredRotation) {
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

		public float ConstraintRotationAgainstParent(int boneIdx, float desiredRotation) {
			float parentRotation = (boneIdx + 1 < bones.Count) ? rotations[boneIdx + 1] : 0;
			float desiredLocalRotation = (float) Math.IEEERemainder(desiredRotation - parentRotation, Math.PI * 2);
			float limit = bones[boneIdx].RotationLimit;
			float localRotation = MathUtil.Clamp(desiredLocalRotation, -limit, +limit);
			return parentRotation + localRotation;
		}

		//From end-effector to root
		public void DoForwardPass() {
			Vector2 targetEnd = firstTargetEnd;

			for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
				Vector2 currentEnd = positions[boneIdx];
				Vector2 currentCenter = positions[boneIdx + 1];
				
				float targetRotationDelta = Vector2Utils.AngleBetween(
					currentEnd - currentCenter,
					targetEnd - currentCenter);
				float unconstrainedTargetRotation = rotations[boneIdx] + targetRotationDelta;
				float targetRotation = ConstrainRotationAgainstChild(boneIdx, unconstrainedTargetRotation);
				Vector2 targetCenter = targetEnd - Vector2Utils.RotateBy(targetRotation, unposedBoneVectors[boneIdx]);
				
				positions[boneIdx] = targetEnd;
				rotations[boneIdx] = targetRotation;
				targetEnd = targetCenter;
			}

			positions[bones.Count] = targetEnd;
		}
		
		// From root to end-effector
		public void DoBackwardPass() {
			Vector2 targetCenter = lastTargetCenter;

			for (int boneIdx = bones.Count - 1; boneIdx >= 0; --boneIdx) {
				Vector2 currentEnd = positions[boneIdx];
				Vector2 currentCenter = positions[boneIdx + 1];
								
				float targetRotationDelta = Vector2Utils.AngleBetween(
					currentEnd - currentCenter,
					currentEnd - targetCenter);
				float unconstrainedTargetRotation = rotations[boneIdx] + targetRotationDelta;
				float targetRotation = ConstraintRotationAgainstParent(boneIdx, unconstrainedTargetRotation);
				Vector2 targetEnd = targetCenter + Vector2Utils.RotateBy(targetRotation, unposedBoneVectors[boneIdx]);
				
				positions[boneIdx + 1] = targetCenter;
				rotations[boneIdx] = targetRotation;
				targetCenter = targetEnd;
			}

			positions[0] = targetCenter;
		}

		public void ApplyToInputs(SkeletonInputs inputs) {
			inputs.Translation = positions[bones.Count] - bones[bones.Count - 1].Center;

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