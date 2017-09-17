using SharpDX;

public class InverseKinematicsAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly InverseKinematicsUserInterface ui;
	private readonly ChannelInputs inputDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureModel model, InverterParameters inverterParameters) {
		this.channelSystem = model.ChannelSystem;
		this.boneSystem = model.BoneSystem;
		this.ui = new InverseKinematicsUserInterface(controllerManager, channelSystem, boneSystem, inverterParameters);
		this.inputDeltas = channelSystem.MakeZeroChannelInputs();

		model.PoseReset += Reset;
	}
	
	private void Reset() {
		inputDeltas.ClearToZero();
	}

	private Vector3 GetCenterPosition(ChannelOutputs outputs, StagedSkinningTransform[] boneTransforms, Bone bone) {
		return boneTransforms[bone.Index].Transform(bone.CenterPoint.GetValue(outputs));
	}
	
	private void ApplyCorrection(ChannelInputs inputs, ChannelOutputs outputs, StagedSkinningTransform[] boneTransforms, Bone bone, Vector3 sourcePosition, Vector3 targetPosition, float weight) {
		var centerPosition = GetCenterPosition(outputs, boneTransforms, bone);

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
	
	public void Update(ChannelInputs inputs, float time, ControlVertexInfo[] previousFrameControlVertexInfos) {
		ChannelInputs baseInputs = new ChannelInputs(inputs);

		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			inputs.RawValues[i] += inputDeltas.RawValues[i];
		}

		InverseKinematicsProblem problem = ui.GetProblem(inputs, previousFrameControlVertexInfos);
		if (problem == null) {
			return;
		}
		
		for (int i = 0; i < 1; ++i) {
			var outputs = channelSystem.Evaluate(null, inputs);
			var boneTransforms = boneSystem.GetBoneTransforms(outputs);
			
			var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.BoneRelativeSourcePosition);

			float weight = 0.5f;
			for (var bone = problem.SourceBone; bone != boneSystem.RootBone && bone.Parent != boneSystem.RootBone; bone = bone.Parent) {
				ApplyCorrection(inputs, outputs, boneTransforms, bone, sourcePosition, problem.TargetPosition, weight);
				weight *= 0.5f;
			}
		}

		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			inputDeltas.RawValues[i] = inputs.RawValues[i] - baseInputs.RawValues[i];
		}
	}
}
