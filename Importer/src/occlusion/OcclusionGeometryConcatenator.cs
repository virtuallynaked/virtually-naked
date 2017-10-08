using System.Linq;

public class OcclusionGeometryConcatenator {
	private SubdivisionMesh combinedMesh;
	private float[] combinedFaceTransparencies;

	public ArraySegment Add(SubdivisionMesh mesh, float[] faceTransparencies) {
		if (combinedMesh == null) {
			combinedMesh = mesh;
			combinedFaceTransparencies = faceTransparencies;

			int count = mesh.Topology.VertexCount;
			return new ArraySegment(0, count);
		} else {
			int startingOffset = combinedMesh.Topology.VertexCount;

			combinedMesh = SubdivisionMesh.Combine(this.combinedMesh, mesh);
			combinedFaceTransparencies = this.combinedFaceTransparencies.Concat(faceTransparencies).ToArray();

			int count = mesh.Topology.VertexCount;
			return new ArraySegment(startingOffset, count);
		}
	}

	public float[] FaceTransparencies => combinedFaceTransparencies;
	public SubdivisionMesh Mesh => combinedMesh;
}
