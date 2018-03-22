using OpenSubdivFacade;
using System;
using System.Linq;

public class SubdivisionTopologyInfo {
	public static readonly SubdivisionTopologyInfo Empty = new SubdivisionTopologyInfo(PackedLists<int>.Empty, new VertexRule[0]);
	
	public PackedLists<int> AdjacentVertices { get; }
	public VertexRule[] VertexRules { get; }

	public SubdivisionTopologyInfo(PackedLists<int> adjacentVertices, VertexRule[] vertexRules) {
		if (adjacentVertices.Count != vertexRules.Length) {
			throw new ArgumentException("count mismatch");
		}

		AdjacentVertices = adjacentVertices;
		VertexRules = vertexRules;
	}

	public static SubdivisionTopologyInfo Combine(SubdivisionTopologyInfo infoA, SubdivisionTopologyInfo infoB) {
		int offset = infoA.VertexRules.Length;

		PackedLists<int> combinedAdjancentVertices = PackedLists<int>.Concat(
			infoA.AdjacentVertices,
			infoB.AdjacentVertices.Map(neighbourIdx => neighbourIdx + offset));

		VertexRule[] combinedVertexRules = Enumerable.Concat(infoA.VertexRules, infoB.VertexRules).ToArray();

		return new SubdivisionTopologyInfo(combinedAdjancentVertices, combinedVertexRules);
	}
}
