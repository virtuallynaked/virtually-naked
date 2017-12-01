using SharpDX;

public class SingleJointInverseKinematicsSolver : IInverseKinematicsSolver {
	private readonly string boneName;

	public SingleJointInverseKinematicsSolver(string boneName) {
		this.boneName = boneName;
	}
		
	public void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		var bone = boneSystem.BonesByName[boneName];
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var boneTransform = boneTransforms[bone.Index];

		var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.UnposedSourcePosition);
		var targetPosition = problem.TargetPosition;

		var centerPosition = boneTransform.Transform(bone.CenterPoint);

		var rotationDelta = QuaternionExtensions.RotateBetween(
			sourcePosition - centerPosition,
			targetPosition - centerPosition);
		
		var localRotation = bone.GetRotation(inputs);
		var totalRotation = boneTransform.Rotation;

		var targetLocalRotation = totalRotation.Chain(rotationDelta).Chain(Quaternion.Invert(totalRotation)).Chain(localRotation);
		
		bone.SetRotation(inputs, targetLocalRotation, true);
	}
}