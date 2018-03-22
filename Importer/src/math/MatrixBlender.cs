using SharpDX;

public class MatrixBlender {
	private Matrix accumulator = Matrix.Zero;

	public void Add(float weight, Matrix matrix) {
		accumulator += weight * matrix;
	}

	public Matrix GetResult() {
		return accumulator;
	}
}
