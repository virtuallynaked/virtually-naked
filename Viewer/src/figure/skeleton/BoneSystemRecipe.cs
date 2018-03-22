using System.Collections.Generic;

public class BoneSystemRecipe {
	public List<BoneRecipe> Bones { get; }

	public BoneSystemRecipe(List<BoneRecipe> bones) {
		Bones = bones;
	}

	public BoneSystem Bake(Dictionary<string, Channel> channelsByName) {
		var bones = new List<Bone>();
		var bonesByName = new Dictionary<string, Bone>();
		Bones.ForEach(recipe => recipe.Bake(channelsByName, bones, bonesByName));
		return new BoneSystem(bones);
	}
}
