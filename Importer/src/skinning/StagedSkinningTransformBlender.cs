public class StagedSkinningTransformBlender {
	private ScalingTransformBlender scalingStageBlender = new ScalingTransformBlender();
	private DualQuaternionBlender rotationStageBlender = new DualQuaternionBlender();

	public void Add(float weight, StagedSkinningTransform t) {
		scalingStageBlender.Add(weight, t.ScalingStage);
		rotationStageBlender.Add(weight, t.RotationStage);
	}

	public StagedSkinningTransform GetResult() {
		return new StagedSkinningTransform(scalingStageBlender.GetResult(), rotationStageBlender.GetResult());
	}
}
