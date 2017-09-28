using SharpDX;

public class InverseKinematicsAnimator {
	private readonly FigureDefinition definition;
	private readonly InverseKinematicsUserInterface ui;
	private readonly ChannelInputs inputDeltas;

	public InverseKinematicsAnimator(ControllerManager controllerManager, FigureDefinition definition, InverterParameters inverterParameters) {
		this.definition = definition;
		this.ui = new InverseKinematicsUserInterface(controllerManager, definition, inverterParameters);
		this.inputDeltas = definition.ChannelSystem.MakeZeroChannelInputs();
	}
	
	public ChannelInputs InputDeltas => inputDeltas;

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
	
	public void Update(ChannelInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		ChannelInputs baseInputs = new ChannelInputs(inputs);

		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			inputs.RawValues[i] += inputDeltas.RawValues[i];
		}

		InverseKinematicsProblem problem = ui.GetProblem(inputs, previousFrameControlVertexInfos);
		if (problem == null) {
			return;
		}
		
		for (int i = 0; i < 1; ++i) {
			var outputs = definition.ChannelSystem.Evaluate(null, inputs);
			var boneTransforms = definition.BoneSystem.GetBoneTransforms(outputs);
			
			var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.BoneRelativeSourcePosition);

			float weight = 0.5f;
			for (var bone = problem.SourceBone; bone != definition.BoneSystem.RootBone && bone.Parent != definition.BoneSystem.RootBone; bone = bone.Parent) {
				ApplyCorrection(inputs, outputs, boneTransforms, bone, sourcePosition, problem.TargetPosition, weight);
				weight *= 0.5f;
			}
		}

		for (int i = 0; i < inputDeltas.RawValues.Length; ++i) {
			inputDeltas.RawValues[i] = inputs.RawValues[i] - baseInputs.RawValues[i];
		}
	}
}
