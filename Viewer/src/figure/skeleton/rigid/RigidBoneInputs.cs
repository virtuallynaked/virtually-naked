using SharpDX;

public class RigidBoneSystemInputs {
	public Vector3 RootTranslation;
	public Vector3[] Rotations { get; }

	public RigidBoneSystemInputs(int boneCount) {
		Rotations = new Vector3[boneCount];
	}

	public RigidBoneSystemInputs(RigidBoneSystemInputs inputs) {
		RootTranslation = inputs.RootTranslation;
		Rotations = (Vector3[]) inputs.Rotations.Clone();
	}

	public void ClearToZero() {
		RootTranslation = Vector3.Zero;
		for (int i = 0; i < Rotations.Length; ++i) {
			Rotations[i] = Vector3.Zero;
		}
	}
}