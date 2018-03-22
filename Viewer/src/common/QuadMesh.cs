using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class QuadMesh {
	public static QuadMesh MakeWithNormalsFromFaces(List<Quad> faces, List<Vector3> vertexPositions) {
		int vertexCount = vertexPositions.Count;
		
		Vector3[] vertexNormals = new Vector3[vertexPositions.Count];

		foreach (Quad face in faces) {
			for (int corner = 0; corner < 4; ++corner) {
				int curIdx = face.GetCorner(corner);

				Vector3 prev = vertexPositions[face.GetCorner(corner - 1)];
				Vector3 cur = vertexPositions[curIdx];
				Vector3 next = vertexPositions[face.GetCorner(corner + 1)];

				Vector3 prevToCur = cur - prev;
				Vector3 curToNext = next - cur;

				Vector3 cross = Vector3.Cross(prevToCur, curToNext);

				vertexNormals[curIdx] += cross;
			}
		}

		for (int i = 0; i < vertexCount; ++i) {
			vertexNormals[i].Normalize();
		}

		return new QuadMesh(faces, vertexPositions, new List<Vector3>(vertexNormals));
	}

	private readonly List<Quad> faces;
	private readonly int vertexCount;
	private readonly List<Vector3> vertexPositions;
	private readonly List<Vector3> vertexNormals;

	public QuadMesh(List<Quad> faces, List<Vector3> vertexPositions, List<Vector3> vertexNormals) {
		this.faces = faces;
		this.vertexCount = vertexPositions.Count;
		this.vertexPositions = vertexPositions;
		this.vertexNormals = vertexNormals;
	}

	public List<Quad> Faces => faces;
	public int VertexCount => vertexCount;
	public List<Vector3> VertexPositions => vertexPositions;
	public List<Vector3> VertexNormals => vertexNormals;

	public QuadMesh Transform(Matrix matrix) {
		List<Vector3> transformedPositions = vertexPositions.Select(v => Vector3.TransformCoordinate(v, matrix)).ToList();

		Matrix3x3 normalMatrix = (Matrix3x3) matrix;
		normalMatrix.Transpose();
		normalMatrix.Invert();
		
		List<Vector3> transformedNormals = vertexNormals.Select(v => Vector3.Transform(v, normalMatrix)).ToList();

		return new QuadMesh(faces, transformedPositions, transformedNormals);
	}

	public QuadMesh Flip() {
		List<Quad> flippedFaces = faces.Select(f => f.Flip()).ToList();
		return new QuadMesh(flippedFaces, vertexPositions, vertexNormals);
	}

	public TriMesh AsTriMesh() {
		//convert quad faces to triangles
		List<Tri> triFaces = new List<Tri>(Faces.Count * 6);
		foreach (Quad face in Faces) {
			triFaces.Add(new Tri(face.Index0, face.Index1, face.Index2));
			triFaces.Add(new Tri(face.Index2, face.Index3, face.Index0));
		}
		
		return new TriMesh(triFaces, VertexPositions, VertexNormals);
	}
}
