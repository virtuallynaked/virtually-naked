class ChannelMenuItem : IRangeMenuItem {
	private readonly string label;
	private readonly ActorModel model;
	private readonly Channel channel;

	public ChannelMenuItem(string label, ActorModel model, Channel channel) {
		this.label = label;
		this.model = model;
		this.channel = channel;
	}
	
	public string Label => label;
	public double Min => channel.Min;
	public double Max => channel.Max;
	public double Value => model.Inputs.RawValues[channel.Index];

	public void SetValue(double value) {
		channel.SetValue(model.Inputs, value);
	}

	public void ResetValue() {
		channel.SetValue(model.Inputs, channel.InitialValue);
	}
}
