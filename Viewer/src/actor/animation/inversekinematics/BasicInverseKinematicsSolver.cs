using SharpDX;
using System.Collections.Generic;

public class BasicInverseKinematicsSolver : IInverseKinematicsSolver {
	private Vector3 GetCenterPosition(DualQuaternion[] boneTransforms, RigidBone bone) {
		return boneTransforms[bone.Index].Transform(bone.CenterPoint);
	}

	private void ApplyCorrection(RigidBoneSystemInputs inputs, DualQuaternion[] boneTransforms, RigidBone bone, ref Vector3 sourcePosition, Vector3 targetPosition, float weight) {
		var centerPosition = GetCenterPosition(boneTransforms, bone);

		var rotationCorrection = QuaternionExtensions.RotateBetween(
			sourcePosition - centerPosition,
			targetPosition - centerPosition);

		var boneTransform = boneTransforms[bone.Index];
		var baseLocalRotation = bone.GetRotation(inputs);
		var localRotationCorrection = Quaternion.Invert(boneTransform.Rotation) * rotationCorrection * boneTransform.Rotation;

		var lerpedRotation = Quaternion.Lerp(
			baseLocalRotation,
			baseLocalRotation * localRotationCorrection,
			weight);
		
		bone.SetRotation(inputs, lerpedRotation, true);

		var newBoneTransform = bone.GetChainedTransform(inputs, bone.Parent != null ? boneTransforms[bone.Parent.Index] : DualQuaternion.Identity);
		var newSourcePosition = newBoneTransform.Transform(boneTransform.InverseTransform(sourcePosition));

		sourcePosition = newSourcePosition;
	}

	private void DoIteration(RigidBoneSystem boneSystem, InverseKinematicsGoal goal, RigidBoneSystemInputs inputs) {
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var sourcePosition = boneTransforms[goal.SourceBone.Index].Transform(goal.UnposedSourcePosition);

		float weight = 0.5f;
		for (var bone = goal.SourceBone; bone != boneSystem.RootBone && bone.Parent != boneSystem.RootBone; bone = bone.Parent) {
			ApplyCorrection(inputs, boneTransforms, bone, ref sourcePosition, goal.TargetPosition, weight);
		}
	}

	public void Solve(RigidBoneSystem boneSystem, List<InverseKinematicsGoal> goals, RigidBoneSystemInputs inputs) {
		for (int i = 0; i < 1; ++i) {
			foreach (var goal in goals) {
				DoIteration(boneSystem, goal, inputs);
			}
		}
	}
}