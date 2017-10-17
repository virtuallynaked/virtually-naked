using SharpDX;

public class InverseKinematicsAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly InverseKinematicsUserInterface ui;

	private ChannelInputs inputDeltas;
	private ChannelInputs transientDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureDefinition definition, InverterParameters inverterParameters) {
		channelSystem = definition.ChannelSystem;
		boneSystem = new RigidBoneSystem(definition.BoneSystem);
		ui = new InverseKinematicsUserInterface(controllerManager, channelSystem, boneSystem, inverterParameters);
		inputDeltas = definition.ChannelSystem.MakeZeroChannelInputs();
	}
	
	public ChannelInputs InputDeltas => inputDeltas;

	public void Reset() {
		inputDeltas.ClearToZero();
	}

	private Vector3 GetCenterPosition(StagedSkinningTransform[] boneTransforms, RigidBone bone) {
		return boneTransforms[bone.Index].Transform(bone.CenterPoint);
	}
	
	private void ApplyCorrection(ChannelInputs inputs, ChannelOutputs outputs, StagedSkinningTransform[] boneTransforms, RigidBone bone, Vector3 sourcePosition, Vector3 targetPosition, float weight) {
		var centerPosition = GetCenterPosition(boneTransforms, bone);

		var rotationCorrection = QuaternionExtensions.RotateBetween(
			sourcePosition - centerPosition,
			targetPosition - centerPosition);

		var boneTransform = boneTransforms[bone.Index];
		var baseLocalRotation = bone.GetRotation(outputs);
		var localRotationCorrection = Quaternion.Invert(boneTransform.RotationStage.Rotation) * rotationCorrection * boneTransform.RotationStage.Rotation;

		var lerpedRotation = Quaternion.Lerp(
			baseLocalRotation,
			baseLocalRotation * localRotationCorrection,
			weight);
		
		bone.SetEffectiveRotation(inputs, outputs, lerpedRotation, SetMask.ApplyClampAndVisibleOnly);
	}
	
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		ChannelInputs baseInputs = new ChannelInputs(inputs);

		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			inputs.RawValues[i] = baseInputs.RawValues[i] + inputDeltas.RawValues[i];
		}
		
		var outputs = channelSystem.Evaluate(null, inputs);
		boneSystem.Synchronize(outputs);

		InverseKinematicsProblem problem = ui.GetProblem(updateParameters, outputs, previousFrameControlVertexInfos);

		if (problem == null) {
			if (transientDeltas != null) {
				inputDeltas = transientDeltas;
				transientDeltas = null;

				//reapply deltas
				for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
					inputs.RawValues[i] = baseInputs.RawValues[i] + inputDeltas.RawValues[i];
				}
			}

			return;
		}
		
		for (int i = 0; i < 1; ++i) {
			var boneTransforms = boneSystem.GetBoneTransforms(outputs);
			
			var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.BoneRelativeSourcePosition);

			float weight = 0.5f;
			for (var bone = problem.SourceBone; bone != boneSystem.RootBone && bone.Parent != boneSystem.RootBone; bone = bone.Parent) {
				ApplyCorrection(inputs, outputs, boneTransforms, bone, sourcePosition, problem.TargetPosition, weight);
				weight *= 0.5f;
			}

			outputs = channelSystem.Evaluate(null, inputs);
		}

		if (transientDeltas == null) {
			transientDeltas = channelSystem.MakeZeroChannelInputs();
		}
		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			transientDeltas.RawValues[i] = inputs.RawValues[i] - baseInputs.RawValues[i];
		}
	}
}
