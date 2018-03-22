using SharpDX;
using System.Collections.Generic;

public class InverseKinematicsAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly IInverseKinematicsGoalProvider goalProvider;
	private readonly IInverseKinematicsSolver solver;

	private RigidBoneSystemInputs poseDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureDefinition definition, InverterParameters inverterParameters) {
		channelSystem = definition.ChannelSystem;
		boneSystem = new RigidBoneSystem(definition.BoneSystem);
		goalProvider = new InverseKinematicsUserInterface(controllerManager, channelSystem, boneSystem, inverterParameters);
		//goalProvider = new DemoInverseKinematicsGoalProvider(boneSystem);
		solver = new HarmonicInverseKinematicsSolver(boneSystem, inverterParameters.BoneAttributes);
		poseDeltas = boneSystem.MakeZeroInputs();
		Reset();
	}
	
	public RigidBoneSystemInputs PoseDeltas => poseDeltas;

	private void SetInitialRotationAngles(string boneName, Vector3 angles) {
		var bone = boneSystem.BonesByName[boneName];
		var twistSwing = bone.RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(angles));
		poseDeltas.Rotations[bone.Index] = twistSwing;
	}

	public void Reset() {
		poseDeltas.ClearNonRoot();
		//SetInitialRotationAngles("lForearmBend", new Vector3(0, -75, 0));
		//SetInitialRotationAngles("lShldrBend", new Vector3(0, 0, -85));
		//SetInitialRotationAngles("lThighBend", new Vector3(0, 0, 85f));
		//SetInitialRotationAngles("lThighTwist", new Vector3(0, 75f, 0));
		//SetInitialRotationAngles("lShin", new Vector3(109, -25, 3.5f));
		//SetInitialRotationAngles("rThighBend", new Vector3(0, 0, -85f));
		//SetInitialRotationAngles("rThighTwist", new Vector3(0, -75f, 0));
	}
		
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs channelInputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		var channelOutputs = channelSystem.Evaluate(null, channelInputs);

		boneSystem.Synchronize(channelOutputs);
		var baseInputs = boneSystem.ReadInputs(channelOutputs);
		var resultInputs = boneSystem.ApplyDeltas(baseInputs, poseDeltas);
		
		List<InverseKinematicsGoal> goals = goalProvider.GetGoals(updateParameters, resultInputs, previousFrameControlVertexInfos);
		
		solver.Solve(boneSystem, goals, resultInputs);
		poseDeltas = boneSystem.CalculateDeltas(baseInputs, resultInputs);
		
		boneSystem.WriteInputs(channelInputs, channelOutputs, resultInputs);
	}
}
