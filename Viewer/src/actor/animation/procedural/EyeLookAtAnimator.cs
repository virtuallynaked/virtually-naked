using SharpDX;
using System;
using static MathExtensions;

public class EyeLookAtAnimator : IProceduralAnimator {
	private static readonly float RotationAngleRejectionThreshold = MathUtil.DegreesToRadians(80);
	
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly BehaviorModel behaviorModel;
	
	private readonly Bone leftEyeBone;
	private readonly Bone rightEyeBone;
	private readonly Bone eyeParentBone;

	private readonly DelayedForecaster<Vector3, Vector3Operators> headPositionForecaster = new DelayedForecaster<Vector3, Vector3Operators>(0.2f, 0.2f, Vector3.Zero);
	
	public EyeLookAtAnimator(ChannelSystem channelSystem, BoneSystem boneSystem, BehaviorModel behaviorModel) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;
		this.behaviorModel = behaviorModel;

		leftEyeBone = boneSystem.BonesByName["lEye"];
		rightEyeBone = boneSystem.BonesByName["rEye"];

		eyeParentBone = leftEyeBone.Parent;
		if (eyeParentBone != rightEyeBone.Parent) {
			throw new Exception("expected eyes to have same parent");
		}
	}
	
	private void UpdateEye(ChannelOutputs outputs, StagedSkinningTransform eyeParentTotalTransform, ChannelInputs inputs, Bone eyeBone, Vector3 targetPosition) {
		Vector3 targetPositionInRotationFreeEyeSpace = eyeParentTotalTransform.InverseTransform(targetPosition * 100) - eyeBone.CenterPoint.GetValue(outputs);

		var targetRotation = QuaternionExtensions.RotateBetween(Vector3.BackwardRH, targetPositionInRotationFreeEyeSpace);
		targetRotation = Quaternion.RotationAxis(
			targetRotation.Axis,
			TukeysBiweight(targetRotation.Angle, RotationAngleRejectionThreshold));

		eyeBone.SetEffectiveRotation(inputs, outputs, targetRotation);
	}

	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		headPositionForecaster.Update(updateParameters.Time, updateParameters.HeadPosition);

		var forecastHeadPosition = headPositionForecaster.Forecast;
		
		if (!behaviorModel.LookAtPlayer) {
			return;
		}

		var outputs = channelSystem.Evaluate(null, inputs);
		var eyeParentTotalTransform = eyeParentBone.GetChainedTransform(outputs);
		
		UpdateEye(outputs, eyeParentTotalTransform, inputs, leftEyeBone, forecastHeadPosition);
		UpdateEye(outputs, eyeParentTotalTransform, inputs, rightEyeBone, forecastHeadPosition);
	}
}
