class ChannelMenuItem : IRangeMenuItem {
	private readonly string label;
	private readonly FigureModel figureModel;
	private readonly Channel channel;

	public ChannelMenuItem(string label, FigureModel figureModel, Channel channel) {
		this.label = label;
		this.figureModel = figureModel;
		this.channel = channel;
	}
	
	public string Label => label;
	public double Min => channel.Min;
	public double Max => channel.Max;
	public double Value => figureModel.Inputs.RawValues[channel.Index];

	public void SetValue(double value) {
		channel.SetValue(figureModel.Inputs, value);
	}

	public void ResetValue() {
		channel.SetValue(figureModel.Inputs, channel.InitialValue);
	}
}
