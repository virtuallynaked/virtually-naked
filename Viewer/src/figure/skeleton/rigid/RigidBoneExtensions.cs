using SharpDX;

public static class RigidBoneExtensions {
	/**
	 * Set only the twist component of a bone's rotation. The original swing is preserved.
	 */
	public static void SetTwistOnly(this RigidBone bone, RigidBoneSystemInputs inputs, Quaternion localRotation) {
		var orientedRotation = bone.OrientationSpace.TransformToOrientedSpace(localRotation);
		TwistSwing twistSwing = TwistSwing.Decompose(bone.RotationOrder.TwistAxis, orientedRotation);
		var originalTwistSwing = inputs.Rotations[bone.Index];
		var twistWithOriginalSwing = new TwistSwing(
			twistSwing.Twist,
			originalTwistSwing.Swing);
		bone.SetOrientedSpaceRotation(inputs, twistWithOriginalSwing, true);
	}

}
