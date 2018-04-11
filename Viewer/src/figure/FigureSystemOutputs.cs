public class FigureSystemOutputs {
	public ChannelOutputs ChannelOutputs { get; }
	public StagedSkinningTransform[] BoneTransforms { get; }

	public FigureSystemOutputs(ChannelOutputs channelOutputs, StagedSkinningTransform[] boneTransforms) {
		ChannelOutputs = channelOutputs;
		BoneTransforms = boneTransforms;
	}
}
