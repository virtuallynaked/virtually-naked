using System;
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

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			RigidBone bone = bones[boneIdx];
			RigidBone parent = bone.Parent;
			DualQuaternion parentTransform = parent != null ? boneTransforms[parent.Source.Index] : DualQuaternion.Identity;
			boneTransforms[boneIdx] = bone.GetChainedTransform(inputs, parentTransform);
		}

		return boneTransforms;
	}
	
	public RigidBoneSystemInputs MakeZeroInputs() {
		return new RigidBoneSystemInputs(bones.Length);
	}

	public RigidBoneSystemInputs ReadInputs(ChannelOutputs channelOutputs) {
		var inputs = new RigidBoneSystemInputs(bones.Length);

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			inputs.Rotations[boneIdx] = bone.Source.Rotation.GetValue(channelOutputs);
		}

		return inputs;
	}
	
	public void WriteInputs(ChannelInputs channelInputs, ChannelOutputs channelOutputs, RigidBoneSystemInputs inputs) {
		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			bone.Source.Rotation.SetEffectiveValue(channelInputs, channelOutputs, inputs.Rotations[boneIdx], SetMask.ApplyClampAndVisibleOnly);
		}
	}

	public RigidBoneSystemInputs SumAndClampInputs(RigidBoneSystemInputs inputsA, RigidBoneSystemInputs inputsB) {
		var sumInputs = new RigidBoneSystemInputs(bones.Length);

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			sumInputs.Rotations[boneIdx] = bone.Constraint.ClampRotation(inputsA.Rotations[boneIdx] + inputsB.Rotations[boneIdx]);
		}

		return sumInputs;
	}

	public RigidBoneSystemInputs CalculateDeltas(RigidBoneSystemInputs sourceInputs, RigidBoneSystemInputs targetInputs) {
		var deltas = new RigidBoneSystemInputs(bones.Length);

		for (int boneIdx = 0; boneIdx < bones.Length; ++boneIdx) {
			var bone = bones[boneIdx];
			deltas.Rotations[boneIdx] = targetInputs.Rotations[boneIdx] - sourceInputs.Rotations[boneIdx];
		}

		return deltas;
	}
}
