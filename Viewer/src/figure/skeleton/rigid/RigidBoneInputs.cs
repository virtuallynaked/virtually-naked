using System;
using SharpDX;

public class RigidBoneSystemInputs {
	public struct RigidBoneInput {
		public Vector3 Rotation { get; set; }
		public Vector3 Translation { get; set; }
	}

	public RigidBoneInput[] BoneInputs { get; }

	public RigidBoneSystemInputs(int boneCount) {
		BoneInputs = new RigidBoneInput[boneCount];
	}

	public RigidBoneSystemInputs(RigidBoneSystemInputs inputs) {
		BoneInputs = (RigidBoneInput[]) inputs.BoneInputs.Clone();
	}

	public void ClearToZero() {
		for (int i = 0; i < BoneInputs.Length; ++i) {
			BoneInputs[i].Rotation = Vector3.Zero;
			BoneInputs[i].Translation = Vector3.Zero;
		}
	}
}