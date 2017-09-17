using SharpDX;

public struct SubQuad {
	public Vector4 W0 { get; }
	public Vector4 W1 { get; }
	public Vector4 W2 { get; }
	public Vector4 W3 { get; }

	public SubQuad(Vector4 w0, Vector4 w1, Vector4 w2, Vector4 w3) {
		W0 = w0;
		W1 = w1;
		W2 = w2;
		W3 = w3;
	}

	private static Vector4[] UnitWeights = new [] {Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW};

	public static SubQuad Whole => new SubQuad(UnitWeights[0], UnitWeights[1], UnitWeights[2], UnitWeights[3]);
	
	public static SubQuad MakeRotatedWhole(int rotation) {
		return new SubQuad(
			UnitWeights[(rotation + 0) % 4],
			UnitWeights[(rotation + 1) % 4],
			UnitWeights[(rotation + 2) % 4],
			UnitWeights[(rotation + 3) % 4]);
	}

	public Vector4 CenterWeight => (W0 + W1 + W2 + W3) / 4;
	
	private static Vector3 ApplyWeights(Vector4 w, PositionedQuad root) {
		return
			w.X * root.P0 +
			w.Y * root.P1 +
			w.Z * root.P2 +
			w.W * root.P3;
	}

	public PositionedQuad AsPositionedQuad(PositionedQuad root) {
		return new PositionedQuad(
			ApplyWeights(W0, root),
			ApplyWeights(W1, root),
			ApplyWeights(W2, root),
			ApplyWeights(W3, root)
		);
	}

	public SubQuad[] Split() {
		Vector4 W01 = (W0 + W1) / 2;
		Vector4 W12 = (W1 + W2) / 2;
		Vector4 W23 = (W2 + W3) / 2;
		Vector4 W30 = (W3 + W0) / 2;
		Vector4 WC = (W01 + W23) / 2;
		return new [] {
			new SubQuad(W0, W01, WC, W30),
			new SubQuad(W01, W1, W12, WC),
			new SubQuad(WC, W12, W2, W23),
			new SubQuad(W30, WC, W23, W3)
		};
	}
}
