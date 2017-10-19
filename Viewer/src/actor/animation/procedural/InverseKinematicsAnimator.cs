using SharpDX;

public class InverseKinematicsAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly InverseKinematicsUserInterface ui;

	private RigidBoneSystemInputs poseDeltas;
	private RigidBoneSystemInputs lastIkDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureDefinition definition, InverterParameters inverterParameters) {
		channelSystem = definition.ChannelSystem;
		boneSystem = new RigidBoneSystem(definition.BoneSystem);
		ui = new InverseKinematicsUserInterface(controllerManager, channelSystem, boneSystem, inverterParameters);
		poseDeltas = boneSystem.MakeZeroInputs();
	}
	
	public RigidBoneSystemInputs PoseDeltas => poseDeltas;

	public void Reset() {
		poseDeltas.ClearToZero();
	}

	private Vector3 GetCenterPosition(StagedSkinningTransform[] boneTransforms, RigidBone bone) {
		return boneTransforms[bone.Index].Transform(bone.CenterPoint);
	}
	
	private void ApplyCorrection(RigidBoneSystemInputs inputs, StagedSkinningTransform[] boneTransforms, RigidBone bone, Vector3 sourcePosition, Vector3 targetPosition, float weight) {
		var centerPosition = GetCenterPosition(boneTransforms, bone);

		var rotationCorrection = QuaternionExtensions.RotateBetween(
			sourcePosition - centerPosition,
			targetPosition - centerPosition);

		var boneTransform = boneTransforms[bone.Index];
		var baseLocalRotation = bone.GetRotation(inputs);
		var localRotationCorrection = Quaternion.Invert(boneTransform.RotationStage.Rotation) * rotationCorrection * boneTransform.RotationStage.Rotation;

		var lerpedRotation = Quaternion.Lerp(
			baseLocalRotation,
			baseLocalRotation * localRotationCorrection,
			weight);
		
		bone.SetRotation(inputs, lerpedRotation, true);
	}
	
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs channelInputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		var channelOutputs = channelSystem.Evaluate(null, channelInputs);

		boneSystem.Synchronize(channelOutputs);
		var baseInputs = boneSystem.ReadInputs(channelOutputs);
		var resultInputs = boneSystem.SumAndClampInputs(baseInputs, poseDeltas);
		
		InverseKinematicsProblem problem = ui.GetProblem(updateParameters, resultInputs, previousFrameControlVertexInfos);

		if (problem == null) {
			if (lastIkDeltas != null) {
				poseDeltas = lastIkDeltas;
				lastIkDeltas = null;

				//reapply deltas
				resultInputs = boneSystem.SumAndClampInputs(baseInputs, poseDeltas);
			}
		} else {
			for (int i = 0; i < 1; ++i) {
				var boneTransforms = boneSystem.GetBoneTransforms(resultInputs);
			
				var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.BoneRelativeSourcePosition);

				float weight = 0.5f;
				for (var bone = problem.SourceBone; bone != boneSystem.RootBone && bone.Parent != boneSystem.RootBone; bone = bone.Parent) {
					ApplyCorrection(resultInputs, boneTransforms, bone, sourcePosition, problem.TargetPosition, weight);
					weight *= 0.5f;
				}
			}

			lastIkDeltas = boneSystem.CalculateDeltas(baseInputs, resultInputs);
		}
		
		boneSystem.WriteInputs(channelInputs, channelOutputs, resultInputs);
	}
}
