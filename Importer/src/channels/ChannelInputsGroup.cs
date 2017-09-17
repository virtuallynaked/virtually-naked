using System.Linq;

public class ChannelInputsGroup {
	public ChannelInputs ParentInputs { get; }
	public ChannelInputs[] ChildInputs { get; }

	public ChannelInputsGroup(ChannelInputs parentInputs, ChannelInputs[] childInputs) {
		ParentInputs = parentInputs;
		ChildInputs = childInputs;
	}

	public ChannelInputsGroup(ChannelInputsGroup group) {
		ParentInputs = new ChannelInputs(group.ParentInputs);
		ChildInputs = group.ChildInputs
			.Select(inputs => new ChannelInputs(inputs))
			.ToArray();
	}
}
