using SharpDX;

public class InverseKinematicsAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly InverseKinematicsUserInterface ui;
	private readonly IInverseKinematicsSolver solver;

	private RigidBoneSystemInputs poseDeltas;
	private RigidBoneSystemInputs lastIkDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureDefinition definition, InverterParameters inverterParameters) {
		channelSystem = definition.ChannelSystem;
		boneSystem = new RigidBoneSystem(definition.BoneSystem);
		ui = new InverseKinematicsUserInterface(controllerManager, channelSystem, boneSystem, inverterParameters);
		solver = new HarmonicInverseKinematicsSolver(inverterParameters.BoneAttributes);
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
		poseDeltas.ClearToZero();
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
		
		InverseKinematicsProblem problem = ui.GetProblem(updateParameters, resultInputs, previousFrameControlVertexInfos);
		
		/*
		var problem = new InverseKinematicsProblem(
			boneSystem.BonesByName["lShin"],
			boneSystem.BonesByName["lFoot"].CenterPoint,
			boneSystem.BonesByName["lFoot"].GetChainedTransform(resultInputs).Transform(boneSystem.BonesByName["lFoot"].CenterPoint));
		*/

		/*
		var forearmBone = boneSystem.BonesByName["lForearmBend"];
		var handBone = boneSystem.BonesByName["lHand"];
		var problem = new InverseKinematicsProblem(
			forearmBone,
			handBone.CenterPoint,
			forearmBone.CenterPoint + Vector3.Down * Vector3.Distance(handBone.CenterPoint, forearmBone.CenterPoint));
		*/
		
		if (problem == null) {
			if (lastIkDeltas != null) {
				poseDeltas = lastIkDeltas;
				lastIkDeltas = null;

				//reapply deltas
				resultInputs = boneSystem.ApplyDeltas(baseInputs, poseDeltas);
			}
		} else {
			solver.Solve(boneSystem, problem, resultInputs);
			poseDeltas = boneSystem.CalculateDeltas(baseInputs, resultInputs);
		}
		
		boneSystem.WriteInputs(channelInputs, channelOutputs, resultInputs);
	}
}
