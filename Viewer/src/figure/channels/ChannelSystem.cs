using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ChannelSystem {
	private readonly ChannelSystem parent;
	private readonly List<Channel> channels;

	private readonly Dictionary<string, Channel> channelsByName;
	private readonly ChannelEvaluator channelEvaluator;

	public ChannelOutputs defaultOutputs;

	public ChannelSystem(ChannelSystem parent, List<Channel> channels) {
		this.parent = parent;
		this.channels = channels;

		this.channelsByName = channels.ToDictionary(channel => channel.Name, channel => channel);
		this.channelEvaluator = new ChannelEvaluator(channels);
		this.defaultOutputs = Evaluate(parent?.defaultOutputs, MakeDefaultChannelInputs());
	}
	
	public ChannelSystem Parent => parent;
	public List<Channel> Channels => channels;
	public Dictionary<string, Channel> ChannelsByName => channelsByName;
	public ChannelOutputs DefaultOutputs => defaultOutputs;

	public ChannelInputs MakeZeroChannelInputs() {
		var initialValues = new double[channels.Count];
		return new ChannelInputs(initialValues);
	}

	public ChannelInputs MakeDefaultChannelInputs() {
		var initialValues = channels
			.Select(channel => channel.InitialValue)
			.ToArray();

		return new ChannelInputs(initialValues);
	}
		
	public ChannelOutputs Evaluate(ChannelOutputs parentOutputs, ChannelInputs inputs) {
		return channelEvaluator.Evaluate(parentOutputs, inputs);
	}
}
