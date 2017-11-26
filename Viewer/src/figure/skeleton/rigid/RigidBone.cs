using SharpDX;

public class RigidBone {
	public Bone Source { get; }
	public int Index { get; }
	public RigidBone Parent { get; }
	public RigidBoneConstraint Constraint { get; }

	public RotationOrder RotationOrder { get; }

	private Vector3 centerPoint;
	private OrientationSpace orientationSpace;

	//only used during synchronization
	private ScalingTransform chainedScalingTransform;
	private Vector3 chainedTranslation;

	public RigidBone(Bone source, RigidBone parent) {
		Source = source;
		Index = source.Index;
		Parent = parent;

		RotationOrder = source.RotationOrder;
		Constraint = RigidBoneConstraint.InitializeFrom(source);
	}

	public Vector3 CenterPoint => centerPoint;

	public void Synchronize(ChannelOutputs outputs) {
		ScalingTransform parentScalingTransform = Parent != null ? Parent.chainedScalingTransform : ScalingTransform.Identity;
		Vector3 parentTranslation = Parent != null ? Parent.chainedTranslation : Vector3.Zero;
		var sourceTranslation = Parent != null ? Source.Translation.GetValue(outputs) : Vector3.Zero; //don't bake in translation for root bone

		chainedScalingTransform = Source.GetObjectCenteredScalingTransform(outputs).Chain(parentScalingTransform);
		chainedTranslation = Vector3.Transform(sourceTranslation, parentScalingTransform.Scale) + parentTranslation;

		Vector3 sourceCenter = Source.CenterPoint.GetValue(outputs);
		centerPoint = parentScalingTransform.Transform(sourceCenter) + chainedTranslation;

		orientationSpace = Source.GetOrientationSpace(outputs);
	}
	
	public Vector3 ConvertRotationToAngles(Quaternion objectSpaceRotation) {
		Quaternion orientatedSpaceRotation = orientationSpace.TransformToOrientedSpace(objectSpaceRotation);

		Vector3 rotationAnglesRadians = RotationOrder.ToAngles(orientatedSpaceRotation);
		Vector3 rotationAnglesDegrees = MathExtensions.RadiansToDegrees(rotationAnglesRadians);

		return rotationAnglesDegrees;
	}

	public Quaternion ConvertAnglesToRotation(Vector3 rotationAngles) {
		Quaternion orientedSpaceRotation = RotationOrder.FromAngles(MathExtensions.DegreesToRadians(rotationAngles));
		Quaternion objectSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);

		return objectSpaceRotation;
	}

	public Quaternion GetRotation(RigidBoneSystemInputs inputs) {
		Vector3 rotationAngles = Constraint.ClampRotation(inputs.Rotations[Index]);
		return ConvertAnglesToRotation(rotationAngles);
	}
	
	public void SetRotation(RigidBoneSystemInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = false) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(objectSpaceRotation);
		if (applyClamp) {
			rotationAnglesDegrees = Constraint.ClampRotation(rotationAnglesDegrees);
		}

		inputs.Rotations[Index] = rotationAnglesDegrees;
	}
			
	private DualQuaternion GetJointCenteredRotationTransform(RigidBoneSystemInputs inputs) {
		Quaternion worldSpaceRotation = GetRotation(inputs);
		return DualQuaternion.FromRotationTranslation(worldSpaceRotation, Vector3.Zero);
	}

	private DualQuaternion GetObjectCenteredRotationTransform(RigidBoneSystemInputs inputs) {
		DualQuaternion localSpaceTransform = GetJointCenteredRotationTransform(inputs);
		return DualQuaternion.FromTranslation(-centerPoint).Chain(localSpaceTransform).Chain(DualQuaternion.FromTranslation(+centerPoint));
	}
	
	public DualQuaternion GetChainedTransform(RigidBoneSystemInputs inputs, DualQuaternion parentTransform) {
		DualQuaternion rotationTransform = GetObjectCenteredRotationTransform(inputs);
		DualQuaternion chainedRotationTransform = rotationTransform.Chain(parentTransform);

		return chainedRotationTransform;
	}

	public DualQuaternion GetChainedTransform(RigidBoneSystemInputs inputs) {
		DualQuaternion rootTransform = DualQuaternion.FromTranslation(inputs.RootTranslation);
		DualQuaternion parentTransform = Parent != null ? Parent.GetChainedTransform(inputs) : rootTransform;
		return GetChainedTransform(inputs, parentTransform);
	}
}
