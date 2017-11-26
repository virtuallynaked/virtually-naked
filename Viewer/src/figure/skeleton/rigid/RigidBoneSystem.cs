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
			inputs.Rotations[boneIdx] = bone.Source.Rotation.GetValue(channelOutputs);
		}

		return inputs;
	}
	
	public void WriteInputs(ChannelInputs channelInputs, ChannelOutputs channelOutputs, RigidBoneSystemInputs inputs) {
		source.RootBone.Translation.SetEffectiveValue(channelInputs, channelOutputs, inputs.RootTranslation, SetMask.ApplyClampAndVisibleOnly);
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			bone.Source.Rotation.SetEffectiveValue(channelInputs, channelOutputs, inputs.Rotations[boneIdx], SetMask.ApplyClampAndVisibleOnly);
		}
	}

	public RigidBoneSystemInputs ApplyDeltas(RigidBoneSystemInputs baseInputs, RigidBoneSystemInputs deltaInputs) {
		var sumInputs = new RigidBoneSystemInputs(bones.Length) {};

		Quaternion baseRotation = RootBone.GetRotation(baseInputs);
		sumInputs.RootTranslation = baseInputs.RootTranslation + Vector3.Transform(deltaInputs.RootTranslation, baseRotation);
		
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			sumInputs.Rotations[boneIdx] = bone.Constraint.ClampRotation(baseInputs.Rotations[boneIdx] + deltaInputs.Rotations[boneIdx]);
		}

		return sumInputs;
	}

	public RigidBoneSystemInputs CalculateDeltas(RigidBoneSystemInputs baseInputs, RigidBoneSystemInputs sumInputs) {
		var deltaInputs = new RigidBoneSystemInputs(bones.Length) {};

		Quaternion baseRotation = RootBone.GetRotation(baseInputs);
		deltaInputs.RootTranslation = Vector3.Transform(sumInputs.RootTranslation - baseInputs.RootTranslation, Quaternion.Invert(baseRotation));
		
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			deltaInputs.Rotations[boneIdx] = sumInputs.Rotations[boneIdx] - baseInputs.Rotations[boneIdx];
		}

		return deltaInputs;
	}
}
