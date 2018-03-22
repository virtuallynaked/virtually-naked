using SharpDX;
using System;

public class PoseBlender {
	private int boneCount;
	private Vector3 rootTranslationAccumulator;
	private Quaternion[] boneRotationAccumulator = null;
	
	public PoseBlender(int boneCount) {
		this.boneCount = boneCount;
		this.rootTranslationAccumulator = Vector3.Zero;
		this.boneRotationAccumulator = new Quaternion[boneCount];
	}

	public void Add(float weight, Pose pose) {
		if (boneRotationAccumulator.Length != boneCount) {
			throw new ArgumentException("bone cout mismatch");
		}

		rootTranslationAccumulator += weight * pose.RootTranslation;

		for (int i = 0; i < boneCount; ++i) {
			float boneWeight = weight;
			if (Quaternion.Dot(boneRotationAccumulator[i], pose.BoneRotations[i]) < 0) {
				boneWeight *= -1;
			}

			boneRotationAccumulator[i] += boneWeight * pose.BoneRotations[i];
		}
	}

	public Pose GetResult() {
		for (int i = 0; i < boneCount; ++i) {
			boneRotationAccumulator[i].Normalize();
		}

		return new Pose(rootTranslationAccumulator, boneRotationAccumulator);
	}
}
