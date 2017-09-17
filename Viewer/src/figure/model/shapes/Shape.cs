public class Shape {
	public static Shape Load(ChannelSystem channelSystem, IArchiveDirectory shapeDirectory) {
		var channelInputsFile = shapeDirectory.File("channel-inputs.dat");
		ChannelInputs channelInputs = Persistance.Load<ChannelInputs>(channelInputsFile);
		return new Shape(shapeDirectory.Name, shapeDirectory, channelInputs);
	}

	public static Shape MakeDefault(ChannelSystem channelSystem) {
		return new Shape("Default", null, channelSystem.MakeDefaultChannelInputs());
	}

	public string Label { get; }
	public ChannelInputs ChannelInputs { get; }
	public IArchiveDirectory Directory { get; }

	public Shape(string label, IArchiveDirectory directory, ChannelInputs channelInputs) {
		Label = label;
		Directory = directory;
		ChannelInputs = channelInputs;
	}
}
