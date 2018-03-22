using SharpDX;

public static class CatmullRomSpline {
	private static readonly Vector4 W0 = new Vector4(0, -1, 2, -1) / 2;
	private static readonly Vector4 W1 = new Vector4(2, 0, -5, 3) / 2;
	private static readonly Vector4 W2 = new Vector4(0, 1, 4, -3) / 2;
	private static readonly Vector4 W3 = new Vector4(0, 0, -1, 1) / 2;

	/**
	 * Returns weights W such that dot(W, P) smoothly interpolates between P[1] at t=0 and P[2] at t=1.
	 */
	public static Vector4 GetWeights(float t) {
		float t0 = 1;
		float t1 = t0 * t;
		float t2 = t1 * t;
		float t3 = t2 * t;
		Vector4 kernel = new Vector4(t0, t1, t2, t3);

		return new Vector4(
			Vector4.Dot(W0, kernel),
			Vector4.Dot(W1, kernel),
			Vector4.Dot(W2, kernel),
			Vector4.Dot(W3, kernel));
	}
}
