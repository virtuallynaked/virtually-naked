using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BoneSystem {
	private readonly List<Bone> bones;

	private readonly Dictionary<string, Bone> bonesByName;

	public BoneSystem(List<Bone> bones) {
		for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
			if (bones[boneIdx].Index != boneIdx) {
				throw new ArgumentException("bone index mismatch");
			}
		}

		this.bones = bones;
		
		this.bonesByName = bones.ToDictionary(bone => bone.Name, bone => bone);
	}
	
	public List<Bone> Bones => bones;
	public Bone RootBone => bones[0];
	public Dictionary<string, Bone> BonesByName => bonesByName;

	public StagedSkinningTransform[] GetBoneTransforms(ChannelOutputs outputs) {
		while (outputs.Parent != null) {
			outputs = outputs.Parent;
		}

		StagedSkinningTransform[] boneTransforms = new StagedSkinningTransform[bones.Count];

		for (int boneIdx = 0; boneIdx < bones.Count; ++boneIdx) {
			Bone bone = bones[boneIdx];
			Bone parent = bone.Parent;
			StagedSkinningTransform parentTransform = parent != null ? boneTransforms[parent.Index] : StagedSkinningTransform.Identity;
			boneTransforms[boneIdx] = bone.GetChainedTransform(outputs, parentTransform);
		}

		return boneTransforms;
	}
}
