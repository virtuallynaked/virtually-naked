using System;
using SharpDX;

public class Bone {
	public string Name {get; }
	public int Index {get; }
	public Bone Parent {get; }
	public RotationOrder RotationOrder {get; }
	public bool InheritsScale {get; }
	public ChannelTriplet CenterPoint {get; }
	public ChannelTriplet EndPoint {get; }
	public ChannelTriplet Orientation {get; }
	public ChannelTriplet Rotation {get; }
	public ChannelTriplet Translation {get; }
	public ChannelTriplet Scale {get; }
	public Channel GeneralScale {get; }

	public TwistSwingConstraint RotationConstraint { get; }

	public Bone(string name, int index, Bone parent, RotationOrder rotationOrder, bool inheritsScale, ChannelTriplet centerPoint, ChannelTriplet endPoint, ChannelTriplet orientation, ChannelTriplet rotation, ChannelTriplet translation, ChannelTriplet scale, Channel generalScale) {
		if (parent == null) {
			if (index != 0) {
				throw new InvalidOperationException("root bone must be first");
			}
		} else if (parent.Index > index) {
			throw new ArgumentException("parent bones must have lower index");
		}

		Name = name;
		Index = index;
		Parent = parent;
		RotationOrder = rotationOrder;
		InheritsScale = inheritsScale;
		CenterPoint = centerPoint;
		EndPoint = endPoint;
		Orientation = orientation;
		Rotation = rotation;
		Translation = translation;
		Scale = scale;
		GeneralScale = generalScale;

		rotation.ExtractMinMax(out Vector3 minDegrees, out Vector3 maxDegrees);
		Vector3 minRadians = MathExtensions.DegreesToRadians(minDegrees);
		Vector3 maxRadians = MathExtensions.DegreesToRadians(maxDegrees);
		RotationConstraint = TwistSwingConstraint.MakeFromRadians(rotationOrder.TwistAxis, minRadians, maxRadians);
	}

	public override string ToString() {
		return $"Bone[{Name}]";
	}

	private Matrix3x3 GetCombinedScale(ChannelOutputs outputs) {
		Vector3 scale = Scale.GetValue(outputs);
		float generalScale = (float) GeneralScale.GetValue(outputs);
		var objectSpaceScaling = Matrix3x3.Scaling(scale * generalScale);

		OrientationSpace orientationSpace = GetOrientationSpace(outputs);
		var orientedSpaceScaling = orientationSpace.TransformToOrientedSpace(objectSpaceScaling);

		return orientedSpaceScaling;
	}

	public OrientationSpace GetOrientationSpace(ChannelOutputs outputs) {
		Vector3 orientationAngles = Orientation.GetValue(outputs);
		Quaternion orientation = RotationOrder.DazStandard.FromEulerAngles(MathExtensions.DegreesToRadians(orientationAngles));
		return new OrientationSpace(orientation);
	}
	
	public RigidTransform GetOriginToBindPoseTransform(ChannelOutputs outputs) {
		Vector3 orientationAngles = Orientation.GetValue(outputs);
		Vector3 centerPoint  = CenterPoint.GetValue(outputs);
		Quaternion orientation = RotationOrder.DazStandard.FromEulerAngles(MathExtensions.DegreesToRadians(orientationAngles));
		return RigidTransform.FromRotationTranslation(orientation, centerPoint);
	}

	public Quaternion GetRotation(ChannelOutputs outputs) {
		OrientationSpace orientationSpace = GetOrientationSpace(outputs);

		Vector3 rotationAngles = Rotation.GetValue(outputs);
		TwistSwing rotationTwistSwing = RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(rotationAngles));
		rotationTwistSwing = RotationConstraint.Clamp(rotationTwistSwing);

		Quaternion orientedSpaceRotation = rotationTwistSwing.AsQuaternion(RotationOrder.TwistAxis);
		Quaternion worldSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);

		return worldSpaceRotation;
	}

	public Vector3 ConvertRotationToAngles(ChannelOutputs orientationOutputs, Quaternion objectSpaceRotation, bool applyClamp) {
		OrientationSpace orientationSpace = GetOrientationSpace(orientationOutputs);
		Quaternion orientatedSpaceRotationQ = orientationSpace.TransformToOrientedSpace(objectSpaceRotation);
		TwistSwing orientatedSpaceRotation = TwistSwing.Decompose(RotationOrder.TwistAxis, orientatedSpaceRotationQ);

		if (applyClamp) {
			orientatedSpaceRotation = RotationConstraint.Clamp(orientatedSpaceRotation);
		}

		Vector3 rotationAnglesRadians = RotationOrder.ToTwistSwingAngles(orientatedSpaceRotation);
		Vector3 rotationAnglesDegrees = MathExtensions.RadiansToDegrees(rotationAnglesRadians);
		
		return rotationAnglesDegrees;
	}

	public void SetRotation(ChannelOutputs orientationOutputs, ChannelInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = true) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(orientationOutputs, objectSpaceRotation, applyClamp);
		Rotation.SetValue(inputs, rotationAnglesDegrees, applyClamp ? SetMask.ApplyClamp : SetMask.Any);
	}

	public void AddRotation(ChannelOutputs orientationOutputs, ChannelInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = true) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(orientationOutputs, objectSpaceRotation, applyClamp);
		Rotation.AddValue(inputs, rotationAnglesDegrees, applyClamp ? SetMask.ApplyClamp : SetMask.Any);
	}

	public void SetEffectiveRotation(ChannelInputs inputs, ChannelOutputs outputs, Quaternion objectSpaceRotation, bool applyClamp = true) {
		Vector3 rotationAnglesDegrees = ConvertRotationToAngles(outputs, objectSpaceRotation, applyClamp);
		Rotation.SetEffectiveValue(inputs, outputs, rotationAnglesDegrees, applyClamp ? SetMask.ApplyClamp : SetMask.Any);
	}
	
	public void SetTranslation(ChannelInputs inputs, Vector3 translation, SetMask mask = SetMask.Any) {
		Translation.SetValue(inputs, translation, mask);
	}

	private ScalingTransform GetJointCenteredScalingTransform(ChannelOutputs outputs) {
		Matrix3x3 scale = GetCombinedScale(outputs);
		
		if (!InheritsScale && Parent != null) {
			scale = Matrix3x3.Invert(Parent.GetCombinedScale(outputs)) * scale;
		}

		return ScalingTransform.FromScale(scale);
	}

	private DualQuaternion GetJointCenteredRotationTransform(ChannelOutputs outputs, Matrix3x3 parentScale) {
		Quaternion worldSpaceRotation = GetRotation(outputs);

		Vector3 translation = Vector3.Transform(Translation.GetValue(outputs), parentScale);
		
		return DualQuaternion.FromRotationTranslation(worldSpaceRotation, translation);
	}
		
	public ScalingTransform GetObjectCenteredScalingTransform(ChannelOutputs outputs) {
		ScalingTransform localSpaceTransform = GetJointCenteredScalingTransform(outputs);
		Vector3 centerPoint  = CenterPoint.GetValue(outputs);
		return ScalingTransform.FromTranslation(-centerPoint).Chain(localSpaceTransform).Chain(ScalingTransform.FromTranslation(+centerPoint));
	}

	private DualQuaternion GetObjectCenteredRotationTransform(ChannelOutputs outputs, ScalingTransform parentScale) {
		DualQuaternion localSpaceTransform = GetJointCenteredRotationTransform(outputs, parentScale.Scale);
		Vector3 centerPoint  = CenterPoint.GetValue(outputs);
		centerPoint = parentScale.Transform(centerPoint);
		return DualQuaternion.FromTranslation(-centerPoint).Chain(localSpaceTransform).Chain(DualQuaternion.FromTranslation(+centerPoint));
	}
	
	public StagedSkinningTransform GetChainedTransform(ChannelOutputs outputs, StagedSkinningTransform parentTransform) {
		ScalingTransform scalingTransform = GetObjectCenteredScalingTransform(outputs);
		ScalingTransform chainedScalingTransform = scalingTransform.Chain(parentTransform.ScalingStage);

		DualQuaternion rotationTransform = GetObjectCenteredRotationTransform(outputs, parentTransform.ScalingStage);
		DualQuaternion chainedRotationTransform = rotationTransform.Chain(parentTransform.RotationStage);

		return new StagedSkinningTransform(chainedScalingTransform, chainedRotationTransform);
	}

	public StagedSkinningTransform GetChainedTransform(ChannelOutputs outputs) {
		StagedSkinningTransform parentTransform = Parent != null ? Parent.GetChainedTransform(outputs) : StagedSkinningTransform.Identity;
		return GetChainedTransform(outputs, parentTransform);
	}
}
