public class StandardProceduralAnimator : IProceduralAnimator {
	private readonly IProceduralAnimator[] animators;
	
	public StandardProceduralAnimator(FigureDefinition definition, BehaviorModel behaviorModel) {
		var channelSystem = definition.ChannelSystem;
		var boneSystem = definition.BoneSystem;

		animators = new IProceduralAnimator[] {
			//new HeadLookAtAnimator(channelSystem, boneSystem),
			new EyeLookAtAnimator(channelSystem, boneSystem, behaviorModel),
			new BreastGravityAnimator(channelSystem, boneSystem),
			new BlinkAnimator(channelSystem),
			//new ExpressionAnimator(channelSystem),
			//new SpeechAnimator(channelSystem, boneSystem)
		};
	}
	
	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		foreach (var animator in animators) {
			animator.Update(updateParameters, inputs);
		}
	}
}
