using SharpDX;

public class ScalingTransformBlender {
	private Matrix3x3 scaleAccumulator = Matrix3x3.Zero;
	private Vector3 translationAccumulator = Vector3.Zero;

	public void Add(float weight, ScalingTransform t) {
		scaleAccumulator += weight * t.Scale;
		translationAccumulator += weight * t.Translation;
	}

	public ScalingTransform GetResult() {
		return new ScalingTransform(scaleAccumulator, translationAccumulator);
	}
}
