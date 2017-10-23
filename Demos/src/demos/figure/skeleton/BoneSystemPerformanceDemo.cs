using System;
using System.Collections.Generic;
using System.Diagnostics;

public class BoneSystemPerformanceDemo : IDemoApp {
	private ChannelSystem channelSystem;
	private BoneSystem boneSystem;
	private ChannelInputs inputs;

	public BoneSystemPerformanceDemo() {
		var figureDir = UnpackedArchiveDirectory.Make(new System.IO.DirectoryInfo("work/figures/genesis-3-female"));

		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		channelSystem = channelSystemRecipe.Bake(null);

		var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
		boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);
		
		var pose = Persistance.Load<List<Pose>>(figureDir.File("animations/idle.dat"))[0];
		inputs = channelSystem.MakeDefaultChannelInputs();
		new Poser(channelSystem, boneSystem).Apply(inputs, pose, DualQuaternion.Identity);
	}

	public void Run() {
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