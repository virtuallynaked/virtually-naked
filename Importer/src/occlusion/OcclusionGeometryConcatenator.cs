using System;
using System.Linq;

public class OcclusionGeometryConcatenator {
	private SubdivisionMesh combinedMesh = SubdivisionMesh.Empty;
	private float[] combinedFaceTransparencies = new float[0];
	private uint[] faceMasks = new uint[0];
	private uint[] vertexMasks = new uint[0];
	private int nextMaskIdx = 0;

	public ArraySegment Add(SubdivisionMesh mesh, float[] faceTransparencies) {
		int startingOffset = combinedMesh.Topology.VertexCount;

		combinedMesh = SubdivisionMesh.Combine(this.combinedMesh, mesh);
		combinedFaceTransparencies = this.combinedFaceTransparencies.Concat(faceTransparencies).ToArray();
		Array.Resize(ref faceMasks, combinedMesh.Topology.Faces.Count());
		Array.Resize(ref vertexMasks, combinedMesh.Topology.VertexCount);

		int count = mesh.Topology.VertexCount;
		return new ArraySegment(startingOffset, count);
	}

	public ArraySegment Add(ImporterOcclusionSurrogate surrogate) {
		int dummyVertexCount = surrogate.SampleCount;
		var dummyMesh = new SubdivisionMesh(
			0,
			new QuadTopology(dummyVertexCount, new Quad[0]),
			PackedLists<WeightedIndexWithDerivatives>.MakeEmptyLists(dummyVertexCount));
		var dummyFaceTransparencies = new float[0];

		var segment = Add(dummyMesh, dummyFaceTransparencies);

		//apply mask
		int maskIdx = nextMaskIdx++;
		uint mask = 1u << maskIdx;
		for (int i = 0; i < segment.Count; ++i) {
			int vertexIdx = i + segment.Offset;
			vertexMasks[vertexIdx] |= mask;
		}
		foreach (int faceIdx in surrogate.AttachedFaces) {
			faceMasks[faceIdx] |= mask;
		}

		return segment;
	}

	public float[] FaceTransparencies => combinedFaceTransparencies;
	public SubdivisionMesh Mesh => combinedMesh;
	public uint[] FaceMasks => faceMasks;
	public uint[] VertexMasks => vertexMasks;
}
