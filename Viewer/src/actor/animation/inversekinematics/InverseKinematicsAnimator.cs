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
		solver = new SingleJointInverseKinematicsSolver("lForearmBend");
		poseDeltas = boneSystem.MakeZeroInputs();
	}
	
	public RigidBoneSystemInputs PoseDeltas => poseDeltas;

	public void Reset() {
		poseDeltas.ClearToZero();
	}
		
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs channelInputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		var channelOutputs = channelSystem.Evaluate(null, channelInputs);

		boneSystem.Synchronize(channelOutputs);
		var baseInputs = boneSystem.ReadInputs(channelOutputs);
		var resultInputs = boneSystem.ApplyDeltas(baseInputs, poseDeltas);
		
		InverseKinematicsProblem problem = ui.GetProblem(updateParameters, resultInputs, previousFrameControlVertexInfos);

		if (problem == null) {
			if (lastIkDeltas != null) {
				poseDeltas = lastIkDeltas;
				lastIkDeltas = null;

				//reapply deltas
				resultInputs = boneSystem.ApplyDeltas(baseInputs, poseDeltas);
			}
		} else {
			solver.Solve(boneSystem, problem, resultInputs);
			lastIkDeltas = boneSystem.CalculateDeltas(baseInputs, resultInputs);
		}
		
		boneSystem.WriteInputs(channelInputs, channelOutputs, resultInputs);
	}
}
