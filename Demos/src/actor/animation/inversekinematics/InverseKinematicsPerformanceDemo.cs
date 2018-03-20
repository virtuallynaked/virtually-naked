using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class InverseKinematicsPerformanceDemo : IDemoApp {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly RigidBoneSystem rigidBoneSystem;
	private readonly IInverseKinematicsGoalProvider goalProvider;
	private readonly IInverseKinematicsSolver solver;
	private readonly RigidBoneSystemInputs initialInputs;

	public InverseKinematicsPerformanceDemo() {
		var figureDir = UnpackedArchiveDirectory.Make(new System.IO.DirectoryInfo("work/figures/genesis-3-female"));
		
		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		channelSystem = channelSystemRecipe.Bake(null);

		var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
		boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);
		
		var inverterParameters = Persistance.Load<InverterParameters>(figureDir.File("inverter-parameters.dat"));

		rigidBoneSystem = new RigidBoneSystem(boneSystem);
		
		goalProvider = new DemoInverseKinematicsGoalProvider(rigidBoneSystem);
		solver = new HarmonicInverseKinematicsSolver(rigidBoneSystem, inverterParameters.BoneAttributes);

		var pose = Persistance.Load<List<Pose>>(figureDir.File("animations/idle.dat"))[0];
		var channelInputs = channelSystem.MakeDefaultChannelInputs();
		new Poser(channelSystem, boneSystem).Apply(channelInputs, pose, DualQuaternion.Identity);
		var channelOutputs = channelSystem.Evaluate(null, channelInputs);

		rigidBoneSystem.Synchronize(channelOutputs);
		initialInputs = rigidBoneSystem.ReadInputs(channelOutputs);
	}

	private void Trial() {
		var inputs = new RigidBoneSystemInputs(initialInputs);
		var frameUpdateParameters = new FrameUpdateParameters(0, 1/90f, null, Vector3.Zero);
		var goals = goalProvider.GetGoals(frameUpdateParameters, initialInputs, null);
		solver.Solve(rigidBoneSystem, goals, inputs);
	}

	public void Run() {
		var stopwatch = Stopwatch.StartNew();
		int trialCount = 0;

		while (true) {
			Trial();

			trialCount += 1;
			if (trialCount == 100) {
				Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds / trialCount);

				trialCount = 0;
				stopwatch.Restart();
			}
		}
	}
}