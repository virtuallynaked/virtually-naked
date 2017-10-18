using SharpDX;

public class RigidBone {
	public Bone Source { get; }
	public int Index { get; }
	public RigidBone Parent { get; }

	public RotationOrder RotationOrder { get; }
	public ChannelTriplet Rotation { get; }
	public ChannelTriplet Translation { get; }

	private Vector3 centerPoint;
	private OrientationSpace orientationSpace;
	private ScalingTransform chainedScalingTransform;

	public RigidBone(Bone source, RigidBone parent) {
		Source = source;
		Index = source.Index;
		Parent = parent;

		RotationOrder = source.RotationOrder;
		Rotation = source.Rotation;
		Translation = source.Translation;
	}

	public Vector3 CenterPoint => centerPoint;

	public void Synchronize(ChannelOutputs outputs) {
		centerPoint = Source.CenterPoint.GetValue(outputs);
		orientationSpace = Source.GetOrientationSpace(outputs);
		chainedScalingTransform = Source.GetObjectCenteredScalingTransform(outputs).Chain(Parent != null ? Parent.chainedScalingTransform : ScalingTransform.Identity);
	}
	
	public Quaternion GetRotation(ChannelOutputs outputs) {
		Vector3 rotationAngles = Rotation.GetValue(outputs);
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

	public void SetRotation(ChannelInputs inputs, Quaternion objectSpaceRotation, SetMask mask = SetMask.Any) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(objectSpaceRotation);
		Rotation.SetValue(inputs, rotationAnglesDegrees, mask);
	}

	public void SetEffectiveRotation(ChannelInputs inputs, ChannelOutputs outputs, Quaternion objectSpaceRotation, SetMask mask = SetMask.Any) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(objectSpaceRotation);
		Rotation.SetEffectiveValue(inputs, outputs, rotationAnglesDegrees, mask);
	}
	
	public void SetTranslation(ChannelInputs inputs, Vector3 translation, SetMask mask = SetMask.Any) {
		Translation.SetValue(inputs, translation, mask);
	}
	
	private DualQuaternion GetJointCenteredRotationTransform(ChannelOutputs outputs, Matrix3x3 parentScale) {
		Vector3 rotationAngles = Rotation.GetValue(outputs);
		Quaternion orientedSpaceRotation = RotationOrder.FromAngles(MathExtensions.DegreesToRadians(rotationAngles));
		Quaternion worldSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);

		Vector3 translation = Vector3.Transform(Translation.GetValue(outputs), parentScale);
		
		return DualQuaternion.FromRotationTranslation(worldSpaceRotation, translation);
	}

	private DualQuaternion GetObjectCenteredRotationTransform(ChannelOutputs outputs, ScalingTransform parentScale) {
		DualQuaternion localSpaceTransform = GetJointCenteredRotationTransform(outputs, parentScale.Scale);
		Vector3 centerPoint = parentScale.Transform(this.centerPoint);
		return DualQuaternion.FromTranslation(-centerPoint).Chain(localSpaceTransform).Chain(DualQuaternion.FromTranslation(+centerPoint));
	}
	
	public StagedSkinningTransform GetChainedTransform(ChannelOutputs outputs, StagedSkinningTransform parentTransform) {
		DualQuaternion rotationTransform = GetObjectCenteredRotationTransform(outputs, parentTransform.ScalingStage);
		DualQuaternion chainedRotationTransform = rotationTransform.Chain(parentTransform.RotationStage);

		return new StagedSkinningTransform(chainedScalingTransform, chainedRotationTransform);
	}

	public StagedSkinningTransform GetChainedTransform(ChannelOutputs outputs) {
		StagedSkinningTransform parentTransform = Parent != null ? Parent.GetChainedTransform(outputs) : StagedSkinningTransform.Identity;
		return GetChainedTransform(outputs, parentTransform);
	}
}
