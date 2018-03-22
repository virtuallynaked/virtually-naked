using System;
using SharpDX;

public struct PositionedQuad {
	public const int CornerCount = 4;

	public static PositionedQuad Make(Vector3[] vertexPositions, Quad quad) {
		return new PositionedQuad(
			vertexPositions[quad.Index0],
			vertexPositions[quad.Index1],
			vertexPositions[quad.Index2],
			vertexPositions[quad.Index3]);
	}

	public Vector3 P0;
	public Vector3 P1;
	public Vector3 P2;
	public Vector3 P3;

	public PositionedQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
		P0 = p0;
		P1 = p1;
		P2 = p2;
		P3 = p3;
	}

	public Vector3 Center => (P0 + P1 + P2 + P3) / 4;

	public BoundingSphere BoundingSphere {
		get {
			Vector3 center = Center;

			float radiusSqr = 0;
			radiusSqr = Math.Max(radiusSqr, Vector3.DistanceSquared(center, P0));
			radiusSqr = Math.Max(radiusSqr, Vector3.DistanceSquared(center, P1));
			radiusSqr = Math.Max(radiusSqr, Vector3.DistanceSquared(center, P2));
			radiusSqr = Math.Max(radiusSqr, Vector3.DistanceSquared(center, P3));
			float radius = (float) Math.Sqrt(radiusSqr);

			return new BoundingSphere(center, radius);
		}
	}

	public double Area {
		get {
			Vector3 v10 = P1 - P0;
			Vector3 v30 = P3 - P0;

			Vector3 v12 = P1 - P2;
			Vector3 v32 = P3 - P2;
		
			return (Vector3.Cross(v10, v30).Length() + Vector3.Cross(v12, v32).Length()) / 2;
		}
	}

	public Vector3 GetCorner(int idx) {
		idx %= 4;
		if (idx < 0) {
			idx += 4;
		}
		switch (idx % 4) {
			case 0: return P0;
			case 1: return P1;
			case 2: return P2;
			default: return P3;
		}
	}

	public int FindClosestCorner(Vector3 point) {
		float bestDist = float.PositiveInfinity;
		int bestIdx = -1;
		for (int idx = 0; idx < CornerCount; ++idx) {
			float dist = Vector3.Distance(point, GetCorner(idx));
			if (dist < bestDist) {
				bestDist = dist;
				bestIdx = idx;
			}
		}
		return bestIdx;
	}
}
