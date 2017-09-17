using System.Linq;

public class QuadTopology {
	public static readonly QuadTopology Empty = new QuadTopology(0, new Quad[0]);

	public int VertexCount { get; }
	public Quad[] Faces { get; }
		
	public QuadTopology(int vertexCount, Quad[] faces) {
		VertexCount = vertexCount;
		Faces = faces;
	}

	public static QuadTopology Combine(QuadTopology topologyA, QuadTopology topologyB) {
		int combinedVertexCount = topologyA.VertexCount + topologyB.VertexCount;
		
		Quad[] combinedFaces = topologyA.Faces
			.Concat(
				topologyB.Faces.Select(face => face.Reindex(topologyA.VertexCount))
			)
			.ToArray();
		
		return new QuadTopology(combinedVertexCount, combinedFaces);
	}
}
