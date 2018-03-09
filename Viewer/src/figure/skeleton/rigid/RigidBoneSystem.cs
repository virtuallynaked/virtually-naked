using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class RigidBoneSystem {
	private readonly BoneSystem source;
	private readonly RigidBone[] bones;
	private readonly Dictionary<string, RigidBone> bonesByName;

	public RigidBoneSystem(BoneSystem source) {
		this.source = source;

		bones = new RigidBone[source.Bones.Count];
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var sourceBone = source.Bones[boneIdx];
			bones[boneIdx] = new RigidBone(source.Bones[boneIdx], sourceBone.Parent != null ? bones[sourceBone.Parent.Index] : null);
		}
				
		bonesByName = bones.ToDictionary(bone => bone.Source.Name, bone => bone);
	}
	
	public RigidBone[] Bones => bones;
	public RigidBone RootBone => bones[0];
	public Dictionary<string, RigidBone> BonesByName => bonesByName;

	public void Synchronize(ChannelOutputs outputs) {
		while (outputs.Parent != null) {
			outputs = outputs.Parent;
		}

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			RigidBone bone = bones[boneIdx];
			bone.Synchronize(outputs);
		}
	}

	public DualQuaternion[] GetBoneTransforms(RigidBoneSystemInputs inputs) {
		DualQuaternion[] boneTransforms = new DualQuaternion[bones.Length];

		DualQuaternion rootTransform = DualQuaternion.FromTranslation(inputs.RootTranslation);

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			RigidBone bone = bones[boneIdx];
			RigidBone parent = bone.Parent;
			DualQuaternion parentTransform = parent != null ? boneTransforms[parent.Source.Index] : rootTransform;
			boneTransforms[boneIdx] = bone.GetChainedTransform(inputs, parentTransform);
		}

		return boneTransforms;
	}
	
	public RigidBoneSystemInputs MakeZeroInputs() {
		return new RigidBoneSystemInputs(bones.Length);
	}

	public RigidBoneSystemInputs ReadInputs(ChannelOutputs channelOutputs) {
		var inputs = new RigidBoneSystemInputs(bones.Length) {};

		inputs.RootTranslation = source.RootBone.Translation.GetValue(channelOutputs);
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			var rotationAngles = bone.Source.Rotation.GetValue(channelOutputs);
			var rotation = bone.RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(rotationAngles));
			inputs.Rotations[boneIdx] = rotation;
		}

		return inputs;
	}
	
	public void WriteInputs(ChannelInputs channelInputs, ChannelOutputs channelOutputs, RigidBoneSystemInputs inputs) {
		source.RootBone.Translation.SetEffectiveValue(channelInputs, channelOutputs, inputs.RootTranslation, SetMask.ApplyClampAndVisibleOnly);
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			var rotation = inputs.Rotations[boneIdx];
			var rotationAngles = MathExtensions.RadiansToDegrees(bone.RotationOrder.ToTwistSwingAngles(rotation));
			bone.Source.Rotation.SetEffectiveValue(channelInputs, channelOutputs, rotationAngles, SetMask.ApplyClampAndVisibleOnly);
		}
	}

	public RigidBoneSystemInputs ApplyDeltas(RigidBoneSystemInputs baseInputs, RigidBoneSystemInputs deltaInputs) {
		var sumInputs = new RigidBoneSystemInputs(bones.Length) {};

		DualQuaternion baseRootTransform = DualQuaternion.FromRotationTranslation(
			RootBone.GetRotation(baseInputs),
			baseInputs.RootTranslation);

		DualQuaternion deltaRootTransform = DualQuaternion.FromRotationTranslation(
			RootBone.GetRotation(deltaInputs),
			deltaInputs.RootTranslation);

		DualQuaternion sumRootTransform = deltaRootTransform.Chain(baseRootTransform);

		sumInputs.RootTranslation = sumRootTransform.Translation;
		RootBone.SetRotation(sumInputs, sumRootTransform.Rotation);
		
		for (int boneIdx = 1; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			sumInputs.Rotations[boneIdx] = bone.Constraint.Clamp(baseInputs.Rotations[boneIdx] + deltaInputs.Rotations[boneIdx]);
		}

		return sumInputs;
	}

	public RigidBoneSystemInputs CalculateDeltas(RigidBoneSystemInputs baseInputs, RigidBoneSystemInputs sumInputs) {
		var deltaInputs = new RigidBoneSystemInputs(bones.Length) {};

		DualQuaternion baseRootTransform = DualQuaternion.FromRotationTranslation(
			RootBone.GetRotation(baseInputs),
			baseInputs.RootTranslation);

		DualQuaternion sumRootTransform = DualQuaternion.FromRotationTranslation(
			RootBone.GetRotation(sumInputs),
			sumInputs.RootTranslation);

		DualQuaternion deltaRootTransform = sumRootTransform.Chain(baseRootTransform.Invert());

		deltaInputs.RootTranslation = deltaRootTransform.Translation;
		RootBone.SetRotation(deltaInputs, deltaRootTransform.Rotation);

		for (int boneIdx = 1; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			deltaInputs.Rotations[boneIdx] = sumInputs.Rotations[boneIdx] - baseInputs.Rotations[boneIdx];
		}

		return deltaInputs;
	}
}
