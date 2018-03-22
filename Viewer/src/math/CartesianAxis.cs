using SharpDX;

public enum CartesianAxis {
	X = 0,
	Y = 1,
	Z = 2
}

public static class CartesianAxes {
	public static CartesianAxis[] Values => new CartesianAxis[] { CartesianAxis.X, CartesianAxis.Y, CartesianAxis.Z };

	private static readonly Vector3[] UnitVectors = { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };

	public static Vector3 AsUnitVector(CartesianAxis axis) {
		return UnitVectors[(int) axis];
	}
}
