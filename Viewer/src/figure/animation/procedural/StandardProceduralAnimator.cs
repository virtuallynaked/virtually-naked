public class StandardProceduralAnimator : IProceduralAnimator {
	private readonly IProceduralAnimator[] animators;
	
	public StandardProceduralAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		animators = new IProceduralAnimator[] {
			//new LookAtAnimator(channelSystem, boneSystem),
			//new BreastPhysicsAnimator(channelSystem, boneSystem),
			new BlinkAnimator(channelSystem),
			//new ExpressionAnimator(channelSystem),
			//new SpeechAnimator(channelSystem, boneSystem)
		};
	}
	
	public void Update(ChannelInputs inputs, float time) {
		foreach (var animator in animators) {
			animator.Update(inputs, time);
		}
	}
}
