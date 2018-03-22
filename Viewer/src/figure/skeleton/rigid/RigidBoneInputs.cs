using SharpDX;

public class RigidBoneSystemInputs {
	public Vector3 RootTranslation;
	public TwistSwing[] Rotations { get; }

	public RigidBoneSystemInputs(int boneCount) {
		Rotations = new TwistSwing[boneCount];
	}

	public RigidBoneSystemInputs(RigidBoneSystemInputs inputs) {
		RootTranslation = inputs.RootTranslation;
		Rotations = (TwistSwing[]) inputs.Rotations.Clone();
	}

	public void ClearToZero() {
		RootTranslation = Vector3.Zero;
		for (int i = 0; i < Rotations.Length; ++i) {
			Rotations[i] = TwistSwing.Zero;
		}
	}

	public void ClearNonRoot() {
		for (int i = 1; i < Rotations.Length; ++i) {
			Rotations[i] = TwistSwing.Zero;
		}
	}
}