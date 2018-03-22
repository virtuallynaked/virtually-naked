public class Graft {
	public struct VertexPair {
		public int Source { get; }
		public int Target { get; }

		public VertexPair(int source, int target) {
			Source = source;
			Target = target;
		}
	}

	public VertexPair[] VertexPairs { get; }
	public int[] HiddenFaces { get; }
	
	public Graft(VertexPair[] vertexPairs, int[] hiddenFaces) {
		VertexPairs = vertexPairs;
		HiddenFaces = hiddenFaces;
	}
}
