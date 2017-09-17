using SharpDX;

public class Binner {
	public enum Mode {
		Midpoints,
		Endpoints
	};
	
	private readonly int dim;
	private readonly Mode mode;

	public Binner(int dim, Mode mode) {
		this.dim = dim;
		this.mode = mode;
	}

	public int Count => dim;

	public float IdxToFloat(int idx) {
		if (mode == Mode.Midpoints) {
			return (idx + 0.5f) / dim;
		} else {
			return (float) idx / (dim - 1);
		}
	}

	public int FloatToIdx(float f) {
		if (mode == Mode.Midpoints) {
			return MathUtil.Clamp((int)(f * dim), 0, dim - 1);
		} else {
			return MathUtil.Clamp((int)(f * (dim - 1) + 0.5f), 0, dim - 1);
		}
	}
}
