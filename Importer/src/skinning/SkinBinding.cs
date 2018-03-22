using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class SkinBinding {
	private readonly List<Bone> bones;
	private readonly PackedLists<BoneWeight> boneWeights;
	private readonly Dictionary<string, string> faceGroupToNodeMap;
	
	public SkinBinding(List<Bone> bones, PackedLists<BoneWeight> boneWeights, Dictionary<string, string> faceGroupToNodeMap) {
		this.bones = bones;
		this.boneWeights = boneWeights;
		this.faceGroupToNodeMap = faceGroupToNodeMap;
	}

	public List<Bone> Bones => bones;
	public PackedLists<BoneWeight> BoneWeights => boneWeights;
	public Dictionary<string, string> FaceGroupToNodeMap => faceGroupToNodeMap;

	public void Apply(StagedSkinningTransform[] allBoneTransforms, Vector3[] vertices) {
		var boneTransforms = bones.Select(bone => allBoneTransforms[bone.Index]).ToArray();

		for (int i = 0; i < boneWeights.Count; ++i) {
			StagedSkinningTransformBlender blender = new StagedSkinningTransformBlender();
			foreach (var boneWeight in boneWeights.GetElements(i)) {
				blender.Add(boneWeight.Weight, boneTransforms[boneWeight.Index]);
			}
			
			Vector3 vertex = vertices[i];
			vertex = blender.GetResult().Transform(vertex);
			vertices[i] = vertex;
		}
	}
}
