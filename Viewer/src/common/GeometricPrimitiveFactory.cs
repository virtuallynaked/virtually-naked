using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using static MathExtensions;

//Ported from https://github.com/Microsoft/DirectXTK/blob/master/Src/Geometry.cpp
public class GeometricPrimitiveFactory {
	public static QuadMesh MakeSphere(float diameter, int tessellation) {
		List<Vector3> vertexPositions = new List<Vector3>();
		List<Vector3> vertexNormals = new List<Vector3>();
		
		if (tessellation < 3) {
			throw new ArgumentOutOfRangeException("tesselation parameter out of range");
		}

		int verticalSegments = tessellation;
		int horizontalSegments = tessellation * 2;

		float radius = diameter / 2;

		for (int i = 0; i <= verticalSegments; i++) {
			float v = 1 - (float)i / verticalSegments;

			float latitude = (i * MathUtil.Pi / verticalSegments) - MathUtil.PiOverTwo;
			float dy = (float) Math.Sin(latitude);
			float dxz = (float) Math.Cos(latitude);

			// Create a single ring of vertices at this latitude.
			for (int j = 0; j <= horizontalSegments; j++) {
				float u = (float)j / horizontalSegments;

				float longitude = j * MathUtil.TwoPi / horizontalSegments;
				float dx = (float) Math.Sin(longitude);
				float dz = (float) Math.Cos(longitude);
				
				dx *= dxz;
				dz *= dxz;

				Vector3 normal = new Vector3(dx, dy, dz);
				Vector2 textureCoordinate = new Vector2(u, v);

				vertexPositions.Add(normal * radius);
				vertexNormals.Add(normal);
			}
		}
		
		// Fill the index buffer with triangles joining each pair of latitude rings.
		List<Quad> faces = new List<Quad>();
		int stride = horizontalSegments + 1;

		for (int i = 0; i < verticalSegments; i++) {
			for (int j = 0; j <= horizontalSegments; j++) {
				int nextI = i + 1;
				int nextJ = (j + 1) % stride;

				Quad face = new Quad(
					i * stride + j,
					i * stride + nextJ,
					nextI * stride + nextJ,
					nextI * stride + j
				);
				faces.Add(face);
			}
		}

		return new QuadMesh(faces, vertexPositions, vertexNormals);
	}
	
	public static QuadMesh MakeCube(float size) {
		// A box has six faces, each one pointing in a different direction.
		const int FaceCount = 6;

		Vector3[] faceNormals = new[] {
			new Vector3(  0,  0,  1 ),
			new Vector3(  0,  0, -1 ),
			new Vector3(  1,  0,  0 ),
			new Vector3( -1,  0,  0 ),
			new Vector3(  0,  1,  0 ),
			new Vector3(  0, -1,  0 ),
		};
		
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Quad> faces = new List<Quad>();

		Vector3 tsize = new Vector3(size / 2);
		
		// Create each face in turn.
		for (int i = 0; i < FaceCount; i++)
		{
			Vector3 normal = faceNormals[i];

			// Get two vectors perpendicular both to the face normal and to each other.
			Vector3 basis = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;

			Vector3 side1 = Vector3.Cross(normal, basis);
			Vector3 side2 = Vector3.Cross(normal, side1);

			// Six indices (two triangles) per face.
			int vbase = vertices.Count;
			faces.Add(new Quad(
				vbase + 0,
				vbase + 1,
				vbase + 2,
				vbase + 3
			));
			
			vertices.Add((normal - side1 - side2) * tsize);
			vertices.Add((normal + side1 - side2) * tsize);
			vertices.Add((normal + side1 + side2) * tsize);
			vertices.Add((normal - side1 + side2) * tsize);

			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
		}

		return new QuadMesh(faces, vertices, normals);
	}
	
	public static QuadMesh MakeQuad(float size) {
		float radius = size / 2;

		List<Vector3> vertices = new List<Vector3>() {
			new Vector3(-1, -1, 0) * radius,
			new Vector3(+1, -1, 0) * radius,
			new Vector3(+1, +1, 0) * radius,
			new Vector3(-1, +1, 0) * radius,
		};

		List<Vector3> normals = new List<Vector3>() {
			Vector3.UnitZ,
			Vector3.UnitZ,
			Vector3.UnitZ,
			Vector3.UnitZ,
		};

		List<Quad> faces = new List<Quad>() {
			new Quad(0, 1, 2, 3)
		};

		return new QuadMesh(faces, vertices, normals);
	}

	public static QuadMesh MakeQuadGrid(float size, int countPerDimension) {
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();

		List<Quad> faces = new List<Quad>();

		float areaPerFace = Sqr(size / countPerDimension);

		for (int i = 0; i < countPerDimension + 1; ++i) {
			float y = (((float) i / countPerDimension) - 0.5f) * size;
			for (int j = 0; j < countPerDimension +1; ++j) {
				float x = (((float) j / countPerDimension) - 0.5f) * size;

				vertices.Add(new Vector3(x, y, 0));
				normals.Add(Vector3.UnitZ);
								
				if (i < countPerDimension && j < countPerDimension) {
					int vertexIdx = vertices.Count - 1;
					int nextXVertexIdx = vertexIdx + 1;
					int nextYVertexIdx = vertexIdx + countPerDimension + 1;
					int nextXYVertexIdx = vertexIdx + countPerDimension + 2;

					faces.Add(new Quad(vertexIdx, nextXVertexIdx, nextXYVertexIdx, nextYVertexIdx));
				}
			}
		}
		
		return new QuadMesh(faces, vertices, normals);
	}

	private class SubdividableVertexCollection {
		private List<Vector3> vertices = new List<Vector3>();
		private Dictionary<(int, int), int> midwayVertexMap = new Dictionary<(int, int), int>();
		
		public int AddControl(Vector3 v) {
			int idx = vertices.Count;
			vertices.Add(v);
			return idx;
		}

		public int AddMidway(int idxA, int idxB) {
			var pair = idxA < idxB ? (idxA, idxB) : (idxB, idxA);
			if (midwayVertexMap.TryGetValue(pair, out int midwayIdx)) {
				return midwayIdx;
			} else {
				midwayIdx = vertices.Count;
				midwayVertexMap.Add(pair, midwayIdx);
				vertices.Add((vertices[idxA] + vertices[idxB]) / 2);
				return midwayIdx;
			}
		}

		public List<Vector3> Vertices => vertices;
	}

	public static TriMesh MakeOctahemisphere(int subdivisionLevel) {
		var vertices = new SubdividableVertexCollection();
		List<Tri> faces;

		{
			//control level
			int center = vertices.AddControl(new Vector3(0, 0, 1));
			int right = vertices.AddControl(new Vector3(+1, 0, 0));
			int up = vertices.AddControl(new Vector3(0, +1, 0));
			int left = vertices.AddControl(new Vector3(-1, 0, 0));
			int down = vertices.AddControl(new Vector3(0, -1, 0));

			faces = new List<Tri> {
				new Tri(center, right, up),
				new Tri(center, up, left),
				new Tri(center, left, down),
				new Tri(center, down, right)
			};
		}
		
		for (int i = 0; i < subdivisionLevel; ++i) {
			var coarseFaces = faces;
			faces = new List<Tri>();
			foreach (Tri face in coarseFaces) {
				int indexA = face.Index0;
				int indexB = face.Index1;
				int indexC = face.Index2;

				int indexAB = vertices.AddMidway(indexA, indexB);
				int indexBC = vertices.AddMidway(indexB, indexC);
				int indexCA = vertices.AddMidway(indexC, indexA);
				
				faces.Add(new Tri(indexA, indexAB, indexCA));
				faces.Add(new Tri(indexAB, indexB, indexBC));
				faces.Add(new Tri(indexCA, indexBC, indexC));
				faces.Add(new Tri(indexBC, indexCA, indexAB));
			}
		}
		
		var normalizedVertices = vertices.Vertices
			.Select(v => Vector3.Normalize(v))
			.ToList();
		
		return new TriMesh(faces, normalizedVertices, normalizedVertices);
	}
}
