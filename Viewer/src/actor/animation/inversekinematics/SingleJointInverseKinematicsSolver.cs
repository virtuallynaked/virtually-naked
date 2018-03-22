using SharpDX;
using System.Collections.Generic;

public class SingleJointInverseKinematicsSolver : IInverseKinematicsSolver {
	private readonly string boneName;

	public SingleJointInverseKinematicsSolver(string boneName) {
		this.boneName = boneName;
	}
		
	private void Solve(RigidBoneSystem boneSystem, InverseKinematicsGoal goal, RigidBoneSystemInputs inputs) {
		var bone = boneSystem.BonesByName[boneName];

		//get bone transforms with the current bone rotation zeroed out
		inputs.Rotations[bone.Index] = TwistSwing.Zero;
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var boneTransform = boneTransforms[bone.Index];

		var worldSourcePosition = boneTransforms[goal.SourceBone.Index].Transform(goal.SourceBone.CenterPoint + goal.UnposedSourcePosition);
		var worldTargetPosition = goal.TargetPosition;
		var worldCenterPosition = boneTransform.Transform(bone.CenterPoint);
		var worldSourceDirection = Vector3.Normalize(worldSourcePosition - worldCenterPosition);
		var worldTargetDirection = Vector3.Normalize(worldTargetPosition - worldCenterPosition);
		
		//transform source and target to bone's oriented space
		var parentTotalRotation = bone.Parent != null ? boneTransforms[bone.Parent.Index].Rotation : Quaternion.Identity;
		var orientedSpaceToWorldTransform = bone.OrientationSpace.Orientation.Chain(parentTotalRotation);
		var worldToOrientatedSpaceTransform = Quaternion.Invert(orientedSpaceToWorldTransform);
		var orientedSourceDirection = Vector3.Transform(worldSourceDirection, worldToOrientatedSpaceTransform);
		var orientedTargetDirection = Vector3.Transform(worldTargetDirection, worldToOrientatedSpaceTransform);
		
		CartesianAxis twistAxis = (CartesianAxis) bone.RotationOrder.primaryAxis;
		var newOrientedRotation = Swing.FromTo(twistAxis, orientedSourceDirection, orientedTargetDirection);
		bone.SetOrientedSpaceRotation(inputs, new TwistSwing(Twist.Zero, newOrientedRotation), true);
	}

	public void Solve(RigidBoneSystem boneSystem, List<InverseKinematicsGoal> goals, RigidBoneSystemInputs inputs) {
		foreach (var goal in goals) {
			Solve(boneSystem, goal, inputs);
		}
	}
}
