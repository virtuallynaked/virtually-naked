using SharpDX;
using System;
using static SharpDX.Vector3;

public static class TangentSpaceUtilities {
	private static float NormalizationFactor(float lengthSquared) {
		return lengthSquared == 0 ? 1 : (float) (1 / Math.Sqrt(lengthSquared));
	}

	private static void Normalize(ref float x, ref float y) {
		float m = NormalizationFactor(x * x + y * y);
		x *= m;
		y *= m;
	}

	/**
	 * Calculate the derivative of position with respect to U given the derivatives of UV and position with
	 * respect to a shared coordinate system (S,T).
	 */
	public static Vector3 CalculatePositionDu(Vector2 dUVdS, Vector2 dUVdT, Vector3 dPdS, Vector3 dPdT) {
		/*
		Derivation in Mathematica:
			p[s_, t_] := p0 + ps*s + pt*t
			u[s_, t_] := u0 + us*s + ut*t
			v[s_, t_] := v0 + vs*s + vt*t
			soln = Solve[u[s, t] == U && v[s, t] == V, {s, t}]
			P = p[s, t] /. soln
			D[P, U] // FullSimplify
			> {(pt*vs - ps*vt)/(ut*vs - us*vt)}
		*/
		
		float us = dUVdS.X;
		float vs = dUVdS.Y;
		float ut = dUVdT.X;
		float vt = dUVdT.Y;
		Normalize(ref us, ref ut);
		Normalize(ref vs, ref vt);
		
		Vector3 ps = dPdS;
		Vector3 pt = dPdT;

		float denom = (ut*vs - us*vt);
		if (denom == 0) {
			return Vector3.Zero;
		}
		Vector3 dPdU = Vector3.Normalize((pt*vs - ps*vt) / denom);
		return dPdU;
	}


	/**
	 * Given two vectors (from1, from2) forming a tangent-plane find a pair of coefficients (a,b) such
	 * that a * from1 + b * from2 == to.
	 * 
	 * Note that the system is overdetermined and 'to' might not lie exactly in a plane. In this case, the least
	 * squares solution is returned.
	 */
	public static Vector2 CalculateTangentSpaceRemappingCoeffs(Vector3 from1, Vector3 from2, Vector3 to) {
		float normalizationFactor1 = NormalizationFactor(from1.LengthSquared());
		float normalizationFactor2 = NormalizationFactor(from2.LengthSquared());

		from1 = from1 * normalizationFactor1;
		from2 = from2 * normalizationFactor2;

		float a11 = Dot(from1, from1);
		float a12 = Dot(from1, from2);
		float a21 = Dot(from2, from1);
		float a22 = Dot(from2, from2);

		float b1 = Dot(from1, to);
		float b2 = Dot(from2, to);

		float det = a11 * a22 - a12 * a21;
		if (MathUtil.IsZero(det)) {
			return Vector2.Zero;
		}

		float m1 = (a22 * b1 - a12 * b2) * normalizationFactor1;
		float m2 = (a11 * b2 - a21 * b1) * normalizationFactor2;
		Normalize(ref m1, ref m2);

		return new Vector2(m1, m2);
	}
}
