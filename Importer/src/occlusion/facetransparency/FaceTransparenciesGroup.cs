public class FaceTransparenciesGroup {
	public float[] Parent { get; }
	public float[][] Children { get; }

	public FaceTransparenciesGroup(float[] parent, params float[][] children) {
		Parent = parent;
		Children = children;
	}
}
