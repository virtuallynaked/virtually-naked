using SharpDX;
using static System.Math;

public class BreastGravityAnimator : IProceduralAnimator {
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	
	private readonly Bone chestBone;

	private readonly Bone lPectoralBone;
	private readonly Bone rPectoralBone;
	private readonly Channel flattenChannel;
	private readonly Channel hangForwardChannel;

	public BreastGravityAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		chestBone = boneSystem.BonesByName["chestUpper"];
		lPectoralBone = boneSystem.BonesByName["lPectoral"];
		rPectoralBone = boneSystem.BonesByName["rPectoral"];
		
		flattenChannel = channelSystem.ChannelsByName["pCTRLBreastsFlatten?value"];
		hangForwardChannel = channelSystem.ChannelsByName["pCTRLBreastsHangForward?value"];
	}
	
	/**
	 *  Quadratic function defined by:
	 *  f(-1) = -1
	 *  f(0) = 0
	 *  f(+1) = 2
	 */
	public static float ExpandPositive(float z) {
		return 0.5f * z * z + 1.5f * z;
	}

	/**
	 *  Quadratic function defined by:
	 *  f(-1) = -2
	 *  f(0) = 0
	 *  f(+1) = +1
	 */
	public static float ExpandNegative(float z) {
		return -ExpandPositive(-z);
	}

	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		var outputs = channelSystem.Evaluate(null, inputs);
		var boneTransforms = boneSystem.GetBoneTransforms(outputs);
		var chestBoneTransform = boneTransforms[chestBone.Index];
		var chestBoneRotation = chestBoneTransform.RotationStage.Rotation;

		chestBoneRotation.Invert();
		var gravity = Vector3.Transform(Vector3.Down, chestBoneRotation);
		
		float xRotation = -5 - gravity.Y * 5;
		lPectoralBone.Rotation.X.SetValue(inputs, xRotation);
		rPectoralBone.Rotation.X.SetValue(inputs, xRotation);

		float yRotationInput = gravity.X;
		//Console.WriteLine(yRotation);
		lPectoralBone.Rotation.Y.SetValue(inputs, 5 * ExpandNegative(yRotationInput));
		rPectoralBone.Rotation.Y.SetValue(inputs, 5 * ExpandPositive(yRotationInput));
		
		float flatten = Max(-gravity.Z, 0);
		float hangForward = Max(+gravity.Z, 0);
		flattenChannel.SetValue(inputs, flatten);
		hangForwardChannel.SetValue(inputs, hangForward);
	}
}
