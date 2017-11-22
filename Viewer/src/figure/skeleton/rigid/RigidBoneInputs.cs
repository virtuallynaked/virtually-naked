using SharpDX;

public class RigidBoneSystemInputs {
	public Vector3[] Rotations { get; }

	public RigidBoneSystemInputs(int boneCount) {
		Rotations = new Vector3[boneCount];
	}

	public RigidBoneSystemInputs(RigidBoneSystemInputs inputs) {
		Rotations = (Vector3[]) inputs.Rotations.Clone();
	}

	public void ClearToZero() {
		for (int i = 0; i < Rotations.Length; ++i) {
			Rotations[i] = Vector3.Zero;
		}
	}
}