using System.Collections.Generic;

public class Shape {
	public const string DefaultLabel = "Default";

	public static List<Shape> LoadAllForFigure(IArchiveDirectory figureDir, ChannelSystem channelSystem) {
		List<Shape> shapes = new List<Shape>();
		
		IArchiveDirectory shapesDirectory = figureDir.Subdirectory("shapes");
		if (shapesDirectory != null) {
			foreach (var shapeDirectory in shapesDirectory.Subdirectories) {
				var shape = Shape.Load(channelSystem, shapeDirectory);
				shapes.Add(shape);
			}
		} else {
			var defaultShape = Shape.MakeDefault(channelSystem);
			shapes.Add(defaultShape);
		}
		
		return shapes;
	}

	public static Shape Load(ChannelSystem channelSystem, IArchiveDirectory shapeDirectory) {
		var channelInputsFile = shapeDirectory.File("channel-inputs.dat");
		var shapeInputsByName = Persistance.Load<Dictionary<string, double>>(channelInputsFile);

		ChannelInputs channelInputs = channelSystem.MakeDefaultChannelInputs();
		foreach (var entry in shapeInputsByName) {
			Channel channel = channelSystem.ChannelsByName[entry.Key];
			channel.SetValue(channelInputs, entry.Value);
		}
		
		return new Shape(shapeDirectory.Name, shapeDirectory, channelInputs);
	}

	public static Shape MakeDefault(ChannelSystem channelSystem) {
		return new Shape(DefaultLabel, null, channelSystem.MakeDefaultChannelInputs());
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
