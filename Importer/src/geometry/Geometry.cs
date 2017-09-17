using OpenSubdivFacade;
using SharpDX;

public class Geometry {
	public GeometryType Type { get; }
	public Quad[] Faces { get; }
	public int[] FaceGroupMap { get; }
	public int[] SurfaceMap { get; }
	public Vector3[] VertexPositions { get; }
	public string[] FaceGroupNames { get; }
	public string[] SurfaceNames { get; }
	public Graft Graft { get; }

	public Geometry(GeometryType type, Quad[] faces, int[] faceGroupMap, int[] surfaceMap, Vector3[] vertexPositions, string[] faceGroupNames, string[] surfaceNames, Graft graft) {
		Type = type;
		Faces = faces;
		FaceGroupMap = faceGroupMap;
		SurfaceMap = surfaceMap;
		VertexPositions = vertexPositions;
		FaceGroupNames = faceGroupNames;
		SurfaceNames = surfaceNames;
		Graft = graft;
	}

	public int VertexCount => VertexPositions.Length;
	public int SurfaceCount => SurfaceNames.Length;

	public PackedLists<WeightedIndex> MakeStencils(StencilKind kind, int refinementLevel) {
		var controlTopology = new QuadTopology(VertexCount, Faces);
		using (var refinement = new Refinement(controlTopology, refinementLevel)) {
			return refinement.GetStencils(kind);
		}
	}

	public MultisurfaceQuadTopology AsTopology() {
		return new MultisurfaceQuadTopology(
			Type,
			VertexCount,
			SurfaceCount,
			Faces,
			SurfaceMap);
	}
}
