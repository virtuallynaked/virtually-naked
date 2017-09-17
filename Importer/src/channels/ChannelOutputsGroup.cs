public class ChannelOutputsGroup {
	public ChannelOutputs ParentOutputs { get; }
	public ChannelOutputs[] ChildOutputs { get; }

	public ChannelOutputsGroup(ChannelOutputs parentOutputs, ChannelOutputs[] childOutputs) {
		ParentOutputs = parentOutputs;
		ChildOutputs = childOutputs;
	}
}
