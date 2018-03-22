using System.Collections.Generic;

public class ChannelOutputs {
	public ChannelOutputs Parent { get; }
	public double[] Values { get; }
	
	public ChannelOutputs(ChannelOutputs parent, double[] values) {
		Parent = parent;
		Values = values;
	}
}
