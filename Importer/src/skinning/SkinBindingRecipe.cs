using ProtoBuf;
using System.Collections.Generic;
using System.Linq;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class SkinBindingRecipe {
	public string[] BoneNames {get; set; }
	public PackedLists<BoneWeight> BoneWeights {get; set; }
	public Dictionary<string, string> FaceGroupToNodeMap {get; set;}

	private static string ResolveName(Dictionary<string, Bone> bones, Dictionary<string, Bone> selfBones, string selfName) {
		string name = selfName;

		while (true) {
			if (bones.ContainsKey(name)) {
				return name;
			}

			name = selfBones[name].Parent.Name;
		}
	}

	public SkinBinding Bake(Dictionary<string, Bone> bones, Dictionary<string, Bone> selfBones) {
		List<Bone> boneList = BoneNames
			.Select(name => ResolveName(bones, selfBones, name))
			.Select(name => bones[name]).ToList();
		return new SkinBinding(boneList, BoneWeights, FaceGroupToNodeMap);
	}
	
	public static SkinBindingRecipe Merge(FigureRecipeMerger.Reindexer reindexer, SkinBindingRecipe parentSkinBinding, SkinBindingRecipe[] childSkinBindings) {
		List<string> mergedBones = new List<string>(parentSkinBinding.BoneNames);
		Dictionary<string, int> mergedBonesIndicesByName = Enumerable.Range(0, mergedBones.Count)
			.ToDictionary(idx => mergedBones[idx], idx => idx);
		
		PackedLists<BoneWeight> mergedBoneWeights = parentSkinBinding.BoneWeights;

		foreach (SkinBindingRecipe childSkinBinding in childSkinBindings) {
			var remappedChildBoneWeights = childSkinBinding.BoneWeights
				.Map(boneWeight => {
					string boneName = childSkinBinding.BoneNames[boneWeight.Index];

					if (!mergedBonesIndicesByName.TryGetValue(boneName, out int mergedBoneIdx)) {
						mergedBoneIdx = mergedBones.Count;
						mergedBonesIndicesByName[boneName] = mergedBoneIdx;
						mergedBones.Add(boneName);
					}
					
					return new BoneWeight(mergedBoneIdx, boneWeight.Weight);
				});
			mergedBoneWeights = PackedLists<BoneWeight>.Concat(mergedBoneWeights, remappedChildBoneWeights);
		}

		return new SkinBindingRecipe {
			BoneNames = mergedBones.ToArray(),
			BoneWeights = mergedBoneWeights,
			FaceGroupToNodeMap = parentSkinBinding.FaceGroupToNodeMap
		};
	}
}
