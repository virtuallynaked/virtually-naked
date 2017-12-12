using SharpDX;

public class RigidBone {
	public Bone Source { get; }
	public int Index { get; }
	public RigidBone Parent { get; }
	public RotationConstraint Constraint { get; }

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
		Constraint = source.RotationConstraint;
	}

	public Vector3 CenterPoint => centerPoint;
	public OrientationSpace OrientationSpace => orientationSpace;

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
	
	public Quaternion GetOrientedSpaceRotation(RigidBoneSystemInputs inputs) {
		Vector3 rotationAngles = Constraint.ClampRotation(inputs.Rotations[Index]);
		Quaternion orientedSpaceRotation = RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(rotationAngles));
		return orientedSpaceRotation;
	}

	public void SetOrientedSpaceRotation(RigidBoneSystemInputs inputs, Quaternion orientatedSpaceRotation, bool applyClamp = false) {
		Vector3 rotationAnglesRadians = RotationOrder.ToTwistSwingAngles(orientatedSpaceRotation);
		Vector3 rotationAnglesDegrees = MathExtensions.RadiansToDegrees(rotationAnglesRadians);
		
		if (applyClamp) {
			rotationAnglesDegrees = Constraint.ClampRotation(rotationAnglesDegrees);
		}

		inputs.Rotations[Index] = rotationAnglesDegrees;
	}

	public Quaternion GetRotation(RigidBoneSystemInputs inputs) {
		Quaternion orientedSpaceRotation = GetOrientedSpaceRotation(inputs);
		Quaternion objectSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);
		return objectSpaceRotation;
	}
	
	public void SetRotation(RigidBoneSystemInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = false) {
		Quaternion orientatedSpaceRotation = orientationSpace.TransformToOrientedSpace(objectSpaceRotation);
		SetOrientedSpaceRotation(inputs, orientatedSpaceRotation, applyClamp);
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
