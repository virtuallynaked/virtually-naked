using System;
using System.Linq;

class PrintBoneHierarchyApp : IDemoApp {
	private ILookup<Bone, Bone> bonesByParent;


	public void Run() {
		var figureDir = UnpackedArchiveDirectory.Make(new System.IO.DirectoryInfo("work/figures/genesis-3-female"));

		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		var channelSystem = channelSystemRecipe.Bake(null);

		var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
		var boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);

		bonesByParent = boneSystem.Bones.ToLookup(bone => bone.Parent);

		foreach (var rootBone in bonesByParent[null]) {
			PrintBone(rootBone, 0);
		}
	}

	private void PrintBone(Bone bone, int level) {
		Console.WriteLine(new String(' ', level * 2) + bone.Name);
		foreach (var child in bonesByParent[bone]) {
			PrintBone(child, level + 1);
		}
	}
}
