using System.Linq;

public class FigureGroup {
	private Figure parent;
	private Figure[] children;
	
	public FigureGroup(Figure parent, params Figure[] children) {
		this.parent = parent;
		this.children = children;
	}
	
	public Figure Parent => parent;
	public Figure[] Children => children;

	public ChannelInputsGroup MakeDefaultChannelInputs() {
		var parentInputs = Parent.MakeDefaultChannelInputs();
		return MakeDefaultChannelInputs(parentInputs);
	}

	public ChannelInputsGroup MakeDefaultChannelInputs(ChannelInputs parentInputs) {
		var childInputs = children
			.Select(figure => figure.MakeDefaultChannelInputs())
			.ToArray();

		return new ChannelInputsGroup(parentInputs, childInputs);
	}

	public ChannelOutputsGroup Evaluate(ChannelInputsGroup inputsGroup) {
		var parentOutputs = Parent.Evaluate(null, inputsGroup.ParentInputs);

		var childOutputs = Enumerable.Zip(children, inputsGroup.ChildInputs,
			(child, childInputs) => child.Evaluate(parentOutputs, childInputs))
			.ToArray();

		return new ChannelOutputsGroup(parentOutputs, childOutputs);
	}
}
