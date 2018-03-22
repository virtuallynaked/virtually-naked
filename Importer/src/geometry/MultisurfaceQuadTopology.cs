using System;

public class MultisurfaceQuadTopology {
	public GeometryType Type { get; }

	public int VertexCount { get; }
	public int SurfaceCount { get; }

	public Quad[] Faces { get; }
	public int[] SurfaceMap;

	public MultisurfaceQuadTopology(GeometryType type, int vertexCount, int surfaceCount, Quad[] faces, int[] surfaceMap) {
		if (faces.Length != surfaceMap.Length) {
			throw new ArgumentException("count mismatch");
		}

		Type = type;
		VertexCount = vertexCount;
		SurfaceCount = surfaceCount;
		Faces = faces;
		SurfaceMap = surfaceMap;
	}
	
	public RefinementResult Refine(int refinementLevel) {
		int actualRefinmentLevel;
		bool derivativesOnly;
		if (Type == GeometryType.SubdivisionSurface) {
			actualRefinmentLevel = refinementLevel;
			derivativesOnly = false;
		} else {
			actualRefinmentLevel = 0;
			derivativesOnly = true;
		}

		RefinementResult refinementResult = RefinementResult.Make(
			new QuadTopology(VertexCount, Faces),
			SurfaceMap,
			actualRefinmentLevel, derivativesOnly);

		return refinementResult;
	}
}
