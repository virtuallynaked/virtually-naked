using SharpDX;

public class RigidBone {
	public Bone Source { get; }
	public int Index { get; }
	public RigidBone Parent { get; }
	public TwistSwingConstraint Constraint { get; }

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

	public override string ToString() {
		return $"RigidBone[{Source.Name}]";
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
	
	public TwistSwing GetOrientedSpaceRotation(RigidBoneSystemInputs inputs) {
		TwistSwing rotationTwistSwing = inputs.Rotations[Index];
		rotationTwistSwing = Constraint.Clamp(rotationTwistSwing);
		return rotationTwistSwing;
	}

	public void SetOrientedSpaceRotation(RigidBoneSystemInputs inputs, TwistSwing orientatedSpaceRotation, bool applyClamp = false) {
		DebugUtilities.AssertFinite(orientatedSpaceRotation);

		if (applyClamp) {
			orientatedSpaceRotation = Constraint.Clamp(orientatedSpaceRotation);
		}
		
		inputs.Rotations[Index] = orientatedSpaceRotation;
	}

	public Quaternion GetRotation(RigidBoneSystemInputs inputs) {
		Quaternion orientedSpaceRotation = GetOrientedSpaceRotation(inputs).AsQuaternion(RotationOrder.TwistAxis);
		Quaternion objectSpaceRotation = orientationSpace.TransformFromOrientedSpace(orientedSpaceRotation);
		return objectSpaceRotation;
	}
	
	public void SetRotation(RigidBoneSystemInputs inputs, Quaternion objectSpaceRotation, bool applyClamp = false) {
		Quaternion orientatedSpaceRotation = orientationSpace.TransformToOrientedSpace(objectSpaceRotation);
		TwistSwing orientedSpaceTwistSwing = TwistSwing.Decompose(RotationOrder.TwistAxis, orientatedSpaceRotation);
		SetOrientedSpaceRotation(inputs, orientedSpaceTwistSwing, applyClamp);
	}
	
	private RigidTransform GetObjectCenteredRotationTransform(RigidBoneSystemInputs inputs) {
		Quaternion rotation = GetRotation(inputs);
		return RigidTransform.FromRotation(rotation, centerPoint);
	}
	
	public RigidTransform GetChainedTransform(RigidBoneSystemInputs inputs, RigidTransform parentTransform) {
		RigidTransform rotationTransform = GetObjectCenteredRotationTransform(inputs);
		RigidTransform chainedRotationTransform = rotationTransform.Chain(parentTransform);

		return chainedRotationTransform;
	}

	public RigidTransform GetChainedTransform(RigidBoneSystemInputs inputs) {
		RigidTransform rootTransform = RigidTransform.FromTranslation(inputs.RootTranslation);
		RigidTransform parentTransform = Parent != null ? Parent.GetChainedTransform(inputs) : rootTransform;
		return GetChainedTransform(inputs, parentTransform);
	}
}
