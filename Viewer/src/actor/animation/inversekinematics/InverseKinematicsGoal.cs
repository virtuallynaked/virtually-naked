using SharpDX;

public class InverseKinematicsGoal {
	public RigidBone SourceBone { get; }
	public Vector3 UnposedSourcePosition { get; }

	public Vector3 TargetPosition { get; }
	
	public InverseKinematicsGoal(RigidBone sourceBone, Vector3 unposedSourcePosition, Vector3 targetPosition) {
		SourceBone = sourceBone;
		UnposedSourcePosition = unposedSourcePosition;
		TargetPosition = targetPosition;
	}
}
