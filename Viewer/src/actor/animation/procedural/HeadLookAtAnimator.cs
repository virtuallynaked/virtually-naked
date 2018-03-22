using SharpDX;
using System;

public class HeadLookAtAnimator : IProceduralAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	
	private readonly Bone headBone;
	private readonly Bone leftEyeBone;
	private readonly Bone rightEyeBone;

	private readonly DelayedForecaster<Vector3, Vector3Operators> headPositionForecaster = new DelayedForecaster<Vector3, Vector3Operators>(0.3f, 1f, Vector3.Zero);
	
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
		headPositionForecaster.Update(updateParameters.Time, updateParameters.HeadPosition);
		var forecastHeadPosition = headPositionForecaster.Forecast;

		var outputs = channelSystem.Evaluate(null, inputs);
		var neckTotalTransform = headBone.Parent.GetChainedTransform(outputs);
				
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
