using SharpDX;

public class RigidBone {
	public Bone Source { get; }
	public int Index { get; }
	public RigidBone Parent { get; }
	public RigidBoneConstraint Constraint { get; }

	public RotationOrder RotationOrder { get; }

	private Vector3 centerPoint;
	private OrientationSpace orientationSpace;
	private ScalingTransform chainedScalingTransform;
	
	public RigidBone(Bone source, RigidBone parent) {
		Source = source;
		Index = source.Index;
		Parent = parent;

		RotationOrder = source.RotationOrder;
		Constraint = RigidBoneConstraint.InitializeFrom(source);
	}

	public Vector3 CenterPoint => centerPoint;

	public void Synchronize(ChannelOutputs outputs) {
		centerPoint = Source.CenterPoint.GetValue(outputs);
		orientationSpace = Source.GetOrientationSpace(outputs);
		chainedScalingTransform = Source.GetObjectCenteredScalingTransform(outputs).Chain(Parent != null ? Parent.chainedScalingTransform : ScalingTransform.Identity);
	}
	
	public Quaternion GetRotation(RigidBoneSystemInputs inputs) {
		Vector3 rotationAngles = Constraint.ClampRotation(inputs.BoneInputs[Index].Rotation);
		Quaternion orientedSpaceRotation = RotationOrder.FromAngles(MathExtensions.DegreesToRadians(rotationAngles));
		Quaternion worldSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);

		return worldSpaceRotation;
	}

	public Vector3 ConvertRotationToAngles(Quaternion objectSpaceRotation) {
		Quaternion orientatedSpaceRotation = orientationSpace.TransformToOrientedSpace(objectSpaceRotation);

		Vector3 rotationAnglesRadians = RotationOrder.ToAngles(orientatedSpaceRotation);
		Vector3 rotationAnglesDegrees = MathExtensions.RadiansToDegrees(rotationAnglesRadians);

		return rotationAnglesDegrees;
	}

	public void SetRotation(RigidBoneSystemInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = false) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(objectSpaceRotation);
		if (applyClamp) {
			rotationAnglesDegrees = Constraint.ClampRotation(rotationAnglesDegrees);
		}

		inputs.BoneInputs[Index].Rotation = rotationAnglesDegrees;
	}
	
	public Vector3 GetTranslation(RigidBoneSystemInputs inputs) {
		return Constraint.ClampTranslation(inputs.BoneInputs[Index].Translation);
	}

	public void SetTranslation(RigidBoneSystemInputs inputs, Vector3 translation, bool applyClamp = false) {
		if (applyClamp) {
			translation = Constraint.ClampRotation(translation);
		}

		inputs.BoneInputs[Index].Translation = translation;
	}
		
	private DualQuaternion GetJointCenteredRotationTransform(RigidBoneSystemInputs inputs, Matrix3x3 parentScale) {
		Quaternion worldSpaceRotation = GetRotation(inputs);
		Vector3 scaledTranslation = Vector3.Transform(GetTranslation(inputs), parentScale);
		return DualQuaternion.FromRotationTranslation(worldSpaceRotation, scaledTranslation);
	}

	private DualQuaternion GetObjectCenteredRotationTransform(RigidBoneSystemInputs inputs, ScalingTransform parentScale) {
		DualQuaternion localSpaceTransform = GetJointCenteredRotationTransform(inputs, parentScale.Scale);
		Vector3 centerPoint = parentScale.Transform(this.centerPoint);
		return DualQuaternion.FromTranslation(-centerPoint).Chain(localSpaceTransform).Chain(DualQuaternion.FromTranslation(+centerPoint));
	}
	
	public StagedSkinningTransform GetChainedTransform(RigidBoneSystemInputs inputs, StagedSkinningTransform parentTransform) {
		DualQuaternion rotationTransform = GetObjectCenteredRotationTransform(inputs, parentTransform.ScalingStage);
		DualQuaternion chainedRotationTransform = rotationTransform.Chain(parentTransform.RotationStage);

		return new StagedSkinningTransform(chainedScalingTransform, chainedRotationTransform);
	}

	public StagedSkinningTransform GetChainedTransform(RigidBoneSystemInputs inputs) {
		StagedSkinningTransform parentTransform = Parent != null ? Parent.GetChainedTransform(inputs) : StagedSkinningTransform.Identity;
		return GetChainedTransform(inputs, parentTransform);
	}
}
