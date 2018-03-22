using SharpDX;

public class InverseKinematicsGoal {
	public RigidBone SourceBone { get; }
	public Vector3 UnposedSourcePosition { get; }
	public Quaternion UnposedSourceOrientation { get; }

	public Vector3 TargetPosition { get; }
	public Quaternion TargetOrientation { get; }
	
	public bool HasOrientation => TargetOrientation != Quaternion.Zero;

	public InverseKinematicsGoal(
		RigidBone sourceBone,
		Vector3 unposedSourcePosition, Quaternion unposedSourceOrientation,
		Vector3 targetPosition, Quaternion targetOrientation) {
		SourceBone = sourceBone;
		UnposedSourcePosition = unposedSourcePosition;
		UnposedSourceOrientation = unposedSourceOrientation;
		TargetPosition = targetPosition;
		TargetOrientation = targetOrientation;
	}

	public InverseKinematicsGoal(
		RigidBone sourceBone, Vector3 unposedSourcePosition,
		Vector3 targetPosition) : this(sourceBone,
			unposedSourcePosition, Quaternion.Zero,
			targetPosition, Quaternion.Zero) {
	}
}
