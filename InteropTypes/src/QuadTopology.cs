using System;
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

	/**
	 * Given two topologies with the same faces but different vertex indices, returns a map from the vertex
	 * indices of one topology to the vertex indices of the other topologies.
	 * 
	 * Each vertex in the source topology must map to a single vertex in the dest topology. However, it's
	 * allowable for multiple source vertices to map to the same dest vertex. Thus it's possible to map
	 * from a source UV topology with seams to a dest spatial topology without seams.
	 */
	public static int[] CalculateVertexIndexMap(QuadTopology sourceTopology, Quad[] destFaces) {
		int faceCount = sourceTopology.Faces.Length;
		if (destFaces.Length != faceCount) {
			throw new InvalidOperationException("face count mismatch");
		}

		int[] indexMap = new int[sourceTopology.VertexCount];
		for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
			Quad sourceFace = sourceTopology.Faces[faceIdx];
			Quad destFace = destFaces[faceIdx];
			
			for (int i = 0; i < Quad.SideCount; ++i) {
				int sourceIdx = sourceFace.GetCorner(i);
				int destIdx = destFace.GetCorner(i);

				int previousMapping = indexMap[sourceIdx];
				if (previousMapping != default(int) && previousMapping != destIdx) {
					throw new InvalidOperationException("mapping conflict");
				}

				indexMap[sourceIdx] = destIdx;
			}
		}

		return indexMap;
	}
}
