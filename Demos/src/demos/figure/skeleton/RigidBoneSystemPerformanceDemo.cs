using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class RigidBoneSystemPerformanceDemo {
	private ChannelSystem channelSystem;
	private BoneSystem boneSystem;
	private RigidBoneSystem rigidBoneSystem;
	private ChannelInputs inputs;

	public RigidBoneSystemPerformanceDemo() {
		var figureDir = UnpackedArchiveDirectory.Make(new System.IO.DirectoryInfo("work/figures/genesis-3-female"));
		
		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		channelSystem = channelSystemRecipe.Bake(null);

		var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
		boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);

		var pose = Persistance.Load<List<Pose>>(figureDir.File("animations/idle.dat"))[0];
		inputs = channelSystem.MakeDefaultChannelInputs();
		new Poser(channelSystem, boneSystem).Apply(inputs, pose, DualQuaternion.Identity);

		rigidBoneSystem = new RigidBoneSystem(boneSystem);
	}

	private void CheckConsistency(ChannelOutputs outputs) {
		var boneTransformsA = boneSystem.GetBoneTransforms(outputs);
		var boneTransformsB = rigidBoneSystem.GetBoneTransforms(outputs);

		for (int i = 0; i < boneSystem.Bones.Count; ++i) {
			var boneTransformA = boneTransformsA[i];
			var boneTransformB = boneTransformsB[i];

			foreach (var testVector in new Vector3[] { Vector3.Zero, Vector3.Right, Vector3.Up, Vector3.BackwardRH }) {
				var transformedVectorA = boneTransformA.Transform(testVector);
				var transformedVectorB = boneTransformB.Transform(testVector);
				float distance = Vector3.Distance(transformedVectorA, transformedVectorB);

				if (distance > 1e-5) {
					throw new Exception("rigid and non-rigid bone transforms are inconsistent");
				}
			}
		}
	}

	public void Run() {
		var outputs = channelSystem.Evaluate(null, inputs);
		rigidBoneSystem.Synchronize(outputs);

		CheckConsistency(outputs);

		var stopwatch = Stopwatch.StartNew();
		int trialCount = 0;

		while (true) {
			rigidBoneSystem.GetBoneTransforms(outputs);

			trialCount += 1;
			if (trialCount == 1000) {
				Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds / trialCount);

				trialCount = 0;
				stopwatch.Restart();
			}
		}
	}
}