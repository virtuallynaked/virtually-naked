using SharpDX;

public interface IVectorOperators<T> {
	T Zero();
	T Add(T t1, T t2);
	T Mul(float f, T t);
}

public struct FloatVectorOperators : IVectorOperators<float> {
	public float Add(float t1, float t2) {
		return t1 + t2;
	}

	public float Mul(float f, float t) {
		return f * t;
	}

	public float Zero() {
		return 0;
	}
}

public struct Vector2Operators : IVectorOperators<Vector2> {
	public Vector2 Add(Vector2 t1, Vector2 t2) {
		return t1 + t2;
	}

	public Vector2 Mul(float f, Vector2 t) {
		return f * t;
	}

	public Vector2 Zero() {
		return Vector2.Zero;
	}
}

public struct Vector3Operators : IVectorOperators<Vector3> {
	public Vector3 Add(Vector3 t1, Vector3 t2) {
		return t1 + t2;
	}

	public Vector3 Mul(float f, Vector3 t) {
		return f * t;
	}

	public Vector3 Zero() {
		return Vector3.Zero;
	}
}
