public class Poser {
	private readonly BoneSystem boneSystem;
	private readonly ChannelOutputs orientationOutputs;

	public Poser(FigureDefinition definition) : this(definition.ChannelSystem, definition.BoneSystem) {
	}

	public Poser(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.boneSystem = boneSystem;
		this.orientationOutputs = channelSystem.DefaultOutputs; //orientation doesn't seem to change between actors so we can use default inputs
	}

	public void Apply(ChannelInputs inputs, Pose pose, DualQuaternion rootTransform) {
		foreach (Bone bone in boneSystem.Bones) {
			bone.AddRotation(orientationOutputs, inputs, pose.BoneRotations[bone.Index]);
		}

		var rescaledRootTransform = DualQuaternion.FromRotationTranslation(rootTransform.Rotation, rootTransform.Translation * 100);
		boneSystem.RootBone.SetRotation(orientationOutputs, inputs, rescaledRootTransform.Rotation);
		boneSystem.RootBone.SetTranslation(inputs, rescaledRootTransform.Translation);

		boneSystem.Bones[1].SetTranslation(inputs, pose.RootTranslation);
	}
}
