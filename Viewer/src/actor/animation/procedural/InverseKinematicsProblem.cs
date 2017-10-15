using SharpDX;

public class InverseKinematicsProblem {
	public RigidBone SourceBone { get; }
	public Vector3 BoneRelativeSourcePosition { get; }

	public Vector3 TargetPosition { get; }
	
	public InverseKinematicsProblem(RigidBone sourceBone, Vector3 boneRelativeSourcePosition, Vector3 targetPosition) {
		SourceBone = sourceBone;
		BoneRelativeSourcePosition = boneRelativeSourcePosition;
		TargetPosition = targetPosition;
	}
}
