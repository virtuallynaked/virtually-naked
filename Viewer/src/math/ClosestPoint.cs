using SharpDX;
using System.Collections.Generic;
using System.Linq;

public static class ClosestPoint {
	public static Vector3 FindClosestPointOnTriangleAsBarycentricWeights(Vector3 a, Vector3 b, Vector3 c, Vector3 p) {
		//From "Real-Time Collision Detection, Volume 1" by Christer Ericson, pg 141

		// Check if P in vertex region outside A
		Vector3 ab = b - a;
		Vector3 ac = c - a;
		Vector3 ap = p - a;
		float d1 = Vector3.Dot(ab, ap);
		float d2 = Vector3.Dot(ac, ap);
		if (d1 <= 0 && d2 <= 0)
			return new Vector3(1, 0, 0);

		// Check if P in vertex region outside B
		Vector3 bp = p - b;
		float d3 = Vector3.Dot(ab, bp);
		float d4 = Vector3.Dot(ac, bp);
		if (d3 >= 0 && d4 <= d3)
			return new Vector3(0, 1, 0);

		// Check if P in edge region of AB, if so return projection of P onto AB
		float vc = d1 * d4 - d3 * d2;
		if (vc <= 0 && d1 >= 0 && d3 <= 0) {
			float v = d1 / (d1 - d3);
			return new Vector3(1 - v, v, 0);
		}

		// Check if P in vertex region outside C
		Vector3 cp = p - c;
		float d5 = Vector3.Dot(ab, cp);
		float d6 = Vector3.Dot(ac, cp);
		if (d6 >= 0 && d5 <= d6)
			return new Vector3(0, 0, 1);

		// Check if P in edge region of AC, if so return projection of P onto AC
		float vb = d5 * d2 - d1 * d6;
		if (vb <= 0 && d2 >= 0 && d6 <= 0) {
			float w = d2 / (d2 - d6);
			return new Vector3(1 - w, 0, w);
		}

		// Check if P in edge region of BC, if so return projection of P onto BC
		float va = d3 * d6 - d5 * d4;
		if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0) {
			float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
			return new Vector3(0, 1 - w, w);
		}

		// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
		{ 
			float denom = 1f / (va + vb + vc);
			float v = vb * denom;
			float w = vc * denom;
			return new Vector3(1 - v - w, v, w);
		}
	}

	public struct PointOnMesh {
		public int VertexIdxA { get; }
		public int VertexIdxB { get; }
		public int VertexIdxC { get; }
		public Vector3 BarycentricWeights { get; }

		public static PointOnMesh MakeClosestToFace(Vector3[] meshVertexPositions, int vertexIdxA, int vertexIdxB, int vertexIdxC, Vector3 targetPoint) {
			Vector3 barycentricWeights = FindClosestPointOnTriangleAsBarycentricWeights(
				meshVertexPositions[vertexIdxA],
				meshVertexPositions[vertexIdxB],
				meshVertexPositions[vertexIdxC],
				targetPoint);
			return new PointOnMesh(vertexIdxA, vertexIdxB, vertexIdxC, barycentricWeights);
		}

		public PointOnMesh(int vertexIdxA, int vertexIdxB, int vertexIdxC, Vector3 barycentricWeights) {
			VertexIdxA = vertexIdxA;
			VertexIdxB = vertexIdxB;
			VertexIdxC = vertexIdxC;
			BarycentricWeights = barycentricWeights;
		}

		public Vector3 AsPosition(Vector3[] meshVertexPositions) {
			return BarycentricWeights.X * meshVertexPositions[VertexIdxA]
				+ BarycentricWeights.Y * meshVertexPositions[VertexIdxB]
				+ BarycentricWeights.Z * meshVertexPositions[VertexIdxC];
		}
	}
	
	public static PointOnMesh FindClosestPointOnMesh(Quad[] meshFaces, Vector3[] meshVertexPositions, Vector3 targetPoint) {
		PointOnMesh bestPointOnMesh = default(PointOnMesh);
		float bestPointOnMeshDist = float.PositiveInfinity;
		
		foreach (Quad quad in meshFaces) {
			for (int i = 0; i < 2; ++i) {
				int vertexIdxA = quad.GetCorner(i + 0);
				int vertexIdxB = quad.GetCorner(i + 1);
				int vertexIdxC = quad.GetCorner(i + 2);
				PointOnMesh closestOnFace = PointOnMesh.MakeClosestToFace(meshVertexPositions, vertexIdxA, vertexIdxB, vertexIdxC, targetPoint);
				Vector3 closestOnFacePosition = closestOnFace.AsPosition(meshVertexPositions);

				float dist = Vector3.DistanceSquared(targetPoint, closestOnFacePosition);
				
				if (dist < bestPointOnMeshDist) {
					bestPointOnMesh = closestOnFace;
					bestPointOnMeshDist = dist;
				}
			}
		}

		return bestPointOnMesh;
	}

	public static int FindClosestFaceOnMesh(Quad[] meshFaces, Vector3[] meshVertexPositions, Vector3 targetPoint) {
		int bestFaceIdx = -1;
		float bestFaceDist = float.PositiveInfinity;
		
		for (int faceIdx = 0; faceIdx < meshFaces.Length; ++faceIdx) {
			Quad quad = meshFaces[faceIdx];
			for (int i = 0; i < 2; ++i) {
				int vertexIdxA = quad.GetCorner(i + 0);
				int vertexIdxB = quad.GetCorner(i + 1);
				int vertexIdxC = quad.GetCorner(i + 2);
				PointOnMesh closestOnFace = PointOnMesh.MakeClosestToFace(meshVertexPositions, vertexIdxA, vertexIdxB, vertexIdxC, targetPoint);
				Vector3 closestOnFacePosition = closestOnFace.AsPosition(meshVertexPositions);

				float dist = Vector3.DistanceSquared(targetPoint, closestOnFacePosition);
				
				if (dist < bestFaceDist) {
					bestFaceIdx = faceIdx;
					bestFaceDist = dist;
				}
			}
		}

		return bestFaceIdx;
	}
}
