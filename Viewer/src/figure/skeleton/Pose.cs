using SharpDX;

public class Pose {
	public Vector3 RootTranslation { get; }
	public Quaternion[] BoneRotations { get; }

	public Pose(Vector3 rootTranslation, Quaternion[] boneRotations) {
		RootTranslation = rootTranslation;
		BoneRotations = boneRotations;
	}

	public static Pose MakeIdentity(int boneCount) {
		Quaternion[] boneRotations = new Quaternion[boneCount];
		for (int i = 0; i < boneCount; ++i) {
			boneRotations[i] = Quaternion.Identity;
		}
		return new Pose(Vector3.Zero, boneRotations);
	}
}
