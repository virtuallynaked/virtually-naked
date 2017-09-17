using SharpDX;

public class BreastPhysicsAnimator : IProceduralAnimator {
	private const float Firmness = 0.3f;

	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	
	private readonly Bone chestBone;
	
	private readonly Channel leftRightChannel;
	private readonly Channel upDownChannel;
	private readonly Channel flattenChannel;
	private readonly Channel hangForwardChannel;

	public BreastPhysicsAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		chestBone = boneSystem.BonesByName["chestUpper"];

		leftRightChannel = channelSystem.ChannelsByName["pCTRLBreastsSide-Side?value"];
		upDownChannel = channelSystem.ChannelsByName["pCTRLBreastsUp-Down?value"];
		flattenChannel = channelSystem.ChannelsByName["pCTRLBreastsFlatten?value"];
		hangForwardChannel = channelSystem.ChannelsByName["pCTRLBreastsHangForward?value"];
	}
	
	public void Update(ChannelInputs inputs, float time) {
		var outputs = channelSystem.Evaluate(null, inputs);
		var boneTransforms = boneSystem.GetBoneTransforms(outputs);
		var chestBoneTransform = boneTransforms[chestBone.Index];
		var chestBoneRotation = chestBoneTransform.RotationStage.Rotation;

		chestBoneRotation.Invert();
		var gravity = Vector3.Transform(Vector3.Down, chestBoneRotation);
		
		float leftRight = -gravity.X * 0.5f;
		float upDown = gravity.Y;
		float flatten = -gravity.Z;
		float hangForward = +gravity.Z;

		float magnitude = 1 - Firmness;
		leftRightChannel.SetValue(inputs, magnitude * leftRight);
		upDownChannel.SetValue(inputs, magnitude * upDown);
		flattenChannel.SetValue(inputs, magnitude * flatten);
		hangForwardChannel.SetValue(inputs, magnitude * hangForward);
	}
}
