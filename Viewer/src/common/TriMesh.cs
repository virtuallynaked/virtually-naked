using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class TriMesh {
	private readonly List<Tri> faces;
	private readonly int vertexCount;
	private readonly List<Vector3> vertexPositions;
	private readonly List<Vector3> vertexNormals;

	public TriMesh(List<Tri> faces, List<Vector3> vertexPositions, List<Vector3> vertexNormals) {
		this.faces = faces;
		this.vertexCount = vertexPositions.Count;
		this.vertexPositions = vertexPositions;
		this.vertexNormals = vertexNormals;
	}

	public List<Tri> Faces => faces;
	public int VertexCount => vertexCount;
	public List<Vector3> VertexPositions => vertexPositions;
	public List<Vector3> VertexNormals => vertexNormals;

	public TriMesh Transform(Matrix matrix) {
		List<Vector3> transformedPositions = vertexPositions.Select(v => Vector3.TransformCoordinate(v, matrix)).ToList();

		Matrix3x3 normalMatrix = (Matrix3x3) matrix;
		normalMatrix.Transpose();
		normalMatrix.Invert();
		
		List<Vector3> transformedNormals = vertexNormals.Select(v => Vector3.Transform(v, normalMatrix)).ToList();

		return new TriMesh(faces, transformedPositions, transformedNormals);
	}

	public TriMesh Flip() {
		List<Tri> flippedFaces = faces.Select(f => f.Flip()).ToList();
		return new TriMesh(flippedFaces, vertexPositions, vertexNormals);
	}

	public QuadMesh AsQuadMesh() {
		return new QuadMesh(
			faces
				.Select(tri => new Quad(tri.Index0, tri.Index1, tri.Index2, tri.Index0))
				.ToList(),
			vertexPositions,
			vertexNormals);
	}
}
