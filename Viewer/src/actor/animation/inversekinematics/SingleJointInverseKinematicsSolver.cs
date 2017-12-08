using SharpDX;

public class SingleJointInverseKinematicsSolver : IInverseKinematicsSolver {
	private const int TwistAxis = Swing.XAxis;

	private readonly string boneName;

	public SingleJointInverseKinematicsSolver(string boneName) {
		this.boneName = boneName;
	}
		
	public void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		var bone = boneSystem.BonesByName[boneName];

		//get bone transforms with the current bone rotation zeroed out
		inputs.Rotations[bone.Index] = Vector3.Zero;
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var boneTransform = boneTransforms[bone.Index];

		var worldSourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.UnposedSourcePosition);
		var worldTargetPosition = problem.TargetPosition;
		var worldCenterPosition = boneTransform.Transform(bone.CenterPoint);
		var worldSourceDirection = Vector3.Normalize(worldSourcePosition - worldCenterPosition);
		var worldTargetDirection = Vector3.Normalize(worldTargetPosition - worldCenterPosition);
		
		//transform source and target to bone's oriented space
		var parentTotalRotation = bone.Parent != null ? boneTransforms[bone.Parent.Index].Rotation : Quaternion.Identity;
		var orientedSpaceToWorldTransform = bone.OrientationSpace.Orientation.Chain(parentTotalRotation);
		var worldToOrientatedSpaceTransform = Quaternion.Invert(orientedSpaceToWorldTransform);
		var orientedSourceDirection = Vector3.Transform(worldSourceDirection, worldToOrientatedSpaceTransform);
		var orientedTargetDirection = Vector3.Transform(worldTargetDirection, worldToOrientatedSpaceTransform);
		
		var newOrientedRotation = Swing.FromTo(TwistAxis, orientedSourceDirection, orientedTargetDirection).AsQuaternion(TwistAxis);
		bone.SetOrientedSpaceRotation(inputs, newOrientedRotation, true);
	}
}