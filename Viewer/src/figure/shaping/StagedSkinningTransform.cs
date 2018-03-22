using SharpDX;

public struct StagedSkinningTransform {
	public static readonly StagedSkinningTransform Identity = new StagedSkinningTransform(ScalingTransform.Identity, DualQuaternion.Identity);

	public ScalingTransform ScalingStage { get; }
	public DualQuaternion RotationStage { get; }

	public StagedSkinningTransform(ScalingTransform scalingStage, DualQuaternion rotationStage) {
		ScalingStage = scalingStage;
		RotationStage = rotationStage;
	}

	public Vector3 Transform(Vector3 v) {
		return RotationStage.Transform(ScalingStage.Transform(v));
	}

	public Vector3 InverseTransform(Vector3 v) {
		return ScalingStage.InverseTransform(RotationStage.InverseTransform(v));
	}
}
