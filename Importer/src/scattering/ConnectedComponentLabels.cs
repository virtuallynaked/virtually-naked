public class ConnectedComponentLabels {
	public static ConnectedComponentLabels Make(int vertexCount, Quad[] faces) {
		var labeller = new ConnectedComponentLabeller(vertexCount, faces);
		labeller.Initialize();
		return new ConnectedComponentLabels(labeller.LabelCount, labeller.VertexLabels, labeller.FaceLabels);
	}

	public int LabelCount { get; }
	public int[] VertexLabels { get; }
	public int[] FaceLabels { get; }

	public ConnectedComponentLabels(int labelCount, int[] vertexLabels, int[] faceLabels) {
		LabelCount = labelCount;
		VertexLabels = vertexLabels;
		FaceLabels = faceLabels;
	}
}
