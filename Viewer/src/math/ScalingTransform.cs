using SharpDX;

public struct ScalingTransform {
	public Matrix3x3 Scale { get; }
	public Vector3 Translation { get; }

	public ScalingTransform(Matrix3x3 scale, Vector3 translation) {
		Scale = scale;
		Translation = translation;
	}

	public static readonly ScalingTransform Identity = new ScalingTransform(Matrix3x3.Identity, Vector3.Zero);

	public static ScalingTransform FromScale(Matrix3x3 scale) {
		return new ScalingTransform(scale, Vector3.Zero);
	}

	public static ScalingTransform FromTranslation(Vector3 translation) {
		return new ScalingTransform(Matrix3x3.Identity, translation);
	}

	public Vector3 Transform(Vector3 v) {
		return Vector3.Transform(v, Scale) + Translation;
	}

	public Vector3 InverseTransform(Vector3 v) {
		return Vector3.Transform(v - Translation, Matrix3x3.Invert(Scale));
	}

	public ScalingTransform Chain(ScalingTransform t2) {
		return new ScalingTransform(Scale * t2.Scale, Vector3.Transform(Translation, t2.Scale) + t2.Translation);
	}
}
