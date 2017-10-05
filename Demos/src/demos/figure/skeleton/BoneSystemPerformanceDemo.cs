using System;
using System.Diagnostics;

public class BoneSystemPerformanceDemo {
	private ChannelSystem channelSystem;
	private BoneSystem boneSystem;

	public BoneSystemPerformanceDemo() {
		var figureDir = UnpackedArchiveDirectory.Make(new System.IO.DirectoryInfo("work/figures/genesis-3-female"));

		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		channelSystem = channelSystemRecipe.Bake(null);

		var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
		boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);
	}

	public void Run() {
		var inputs = channelSystem.MakeDefaultChannelInputs();
		var outputs = channelSystem.Evaluate(null, inputs);

		var stopwatch = Stopwatch.StartNew();
		int trialCount = 0;

		while (true) {
			boneSystem.GetBoneTransforms(outputs);

			trialCount += 1;
			if (trialCount == 1000) {
				Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds / trialCount);

				trialCount = 0;
				stopwatch.Restart();
			}
		}
	}
}