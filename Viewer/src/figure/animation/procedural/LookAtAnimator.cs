using SharpDX;
using System;
using Valve.VR;

public class LookAtAnimator : IProceduralAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	
	private readonly Bone headBone;
	private readonly Bone leftEyeBone;
	private readonly Bone rightEyeBone;
	
	public LookAtAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		headBone = boneSystem.BonesByName["head"];
		leftEyeBone = boneSystem.BonesByName["lEye"];
		rightEyeBone = boneSystem.BonesByName["rEye"];

		if (leftEyeBone.Parent != headBone || rightEyeBone.Parent != headBone) {
			throw new InvalidOperationException("expected parent of eyes to be head");
		}
	}
	
	private static Vector3 GetHmdEyePosition() {
		
		TrackedDevicePose_t pose = default(TrackedDevicePose_t);
		TrackedDevicePose_t gamePose = default(TrackedDevicePose_t);
		OpenVR.Compositor.GetLastPoseForTrackedDeviceIndex(OpenVR.k_unTrackedDeviceIndex_Hmd, ref pose, ref gamePose);
		Matrix hmdToWorldTransform = gamePose.mDeviceToAbsoluteTracking.Convert();

		Matrix eyeTransform = Matrix.Translation(0, 0, +0.05f);

		Matrix meanEyeToWorldTransform = eyeTransform * hmdToWorldTransform;

		return meanEyeToWorldTransform.TranslationVector;
	}

	private const float LagAmount = 0.04f;
	private Vector3 laggingHmdWorldPosition = Vector3.Zero;
	private Vector3 halfLaggingHmdWorldPosition = Vector3.Zero;
	
	//private Quaternion laggingLocalRotationCorrection = Quaternion.Identity;

	public void Update(ChannelInputs inputs, float time) {
		var outputs = channelSystem.Evaluate(null, inputs);
		var boneTotalTransforms = boneSystem.GetBoneTransforms(outputs);
		var neckTotalTransform = boneTotalTransforms[headBone.Parent.Index];
		
		var hmdWorldPosition = GetHmdEyePosition() * 100;
		laggingHmdWorldPosition = Vector3.Lerp(laggingHmdWorldPosition, hmdWorldPosition, LagAmount);
		halfLaggingHmdWorldPosition = Vector3.Lerp(halfLaggingHmdWorldPosition, hmdWorldPosition, LagAmount * 2);
		var forecastHmdWorldPosition = halfLaggingHmdWorldPosition - (laggingHmdWorldPosition - halfLaggingHmdWorldPosition);
		
		var figureEyeCenter = (leftEyeBone.CenterPoint.GetValue(outputs) + rightEyeBone.CenterPoint.GetValue(outputs)) / 2;
		var figureEyeWorldPosition = neckTotalTransform.Transform(figureEyeCenter);
		var lookPointWorldPosition = neckTotalTransform.Transform(figureEyeCenter + Vector3.BackwardRH);
		var lookWorldDirection = Vector3.Normalize(lookPointWorldPosition - figureEyeWorldPosition);
		var targetLookWorldDirection = Vector3.Normalize(forecastHmdWorldPosition - figureEyeWorldPosition);

		var worldRotationCorrection = QuaternionExtensions.RotateBetween(lookWorldDirection, targetLookWorldDirection);

		var targetLocalRotationCorrection = Quaternion.Invert(neckTotalTransform.RotationStage.Rotation) * worldRotationCorrection * neckTotalTransform.RotationStage.Rotation;
		
		//laggingLocalRotationCorrection = Quaternion.Lerp(laggingLocalRotationCorrection, targetLocalRotationCorrection, 0.05f);

		headBone.SetEffectiveRotation(inputs, outputs, targetLocalRotationCorrection);
	}
}
