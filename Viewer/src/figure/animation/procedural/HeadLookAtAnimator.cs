using SharpDX;
using System;

public class HeadLookAtAnimator : IProceduralAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	
	private readonly Bone headBone;
	private readonly Bone leftEyeBone;
	private readonly Bone rightEyeBone;

	private readonly LaggedVector3Forecaster headPositionForecaster = new LaggedVector3Forecaster(0.06f);
	
	public HeadLookAtAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		headBone = boneSystem.BonesByName["head"];
		leftEyeBone = boneSystem.BonesByName["lEye"];
		rightEyeBone = boneSystem.BonesByName["rEye"];

		if (leftEyeBone.Parent != headBone || rightEyeBone.Parent != headBone) {
			throw new InvalidOperationException("expected parent of eyes to be head");
		}
	}
		
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		headPositionForecaster.Update(updateParameters.HeadPosition);
		var forecastHeadPosition = headPositionForecaster.ForecastValue;

		var outputs = channelSystem.Evaluate(null, inputs);
		var boneTotalTransforms = boneSystem.GetBoneTransforms(outputs);
		var neckTotalTransform = boneTotalTransforms[headBone.Parent.Index];
				
		var figureEyeCenter = (leftEyeBone.CenterPoint.GetValue(outputs) + rightEyeBone.CenterPoint.GetValue(outputs)) / 2;
		var figureEyeWorldPosition = neckTotalTransform.Transform(figureEyeCenter);
		var lookPointWorldPosition = neckTotalTransform.Transform(figureEyeCenter + Vector3.BackwardRH);
		var lookWorldDirection = Vector3.Normalize(lookPointWorldPosition - figureEyeWorldPosition);
		var targetLookWorldDirection = Vector3.Normalize(forecastHeadPosition * 100 - figureEyeWorldPosition);

		var worldRotationCorrection = QuaternionExtensions.RotateBetween(lookWorldDirection, targetLookWorldDirection);

		var targetLocalRotationCorrection = Quaternion.Invert(neckTotalTransform.RotationStage.Rotation) * worldRotationCorrection * neckTotalTransform.RotationStage.Rotation;
		
		headBone.SetEffectiveRotation(inputs, outputs, targetLocalRotationCorrection);
	}
}
