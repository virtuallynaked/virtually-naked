using System.Collections.Generic;

public class Shape {
	public static List<Shape> LoadAllForFigure(IArchiveDirectory figureDir, ChannelSystem channelSystem) {
		List<Shape> shapes = new List<Shape>();
		
		IArchiveDirectory shapesDirectory = figureDir.Subdirectory("shapes");
		if (shapesDirectory != null) {
			foreach (var shapeDirectory in shapesDirectory.Subdirectories) {
				var shape = Shape.Load(shapeDirectory);
				shapes.Add(shape);
			}
		} else {
			var defaultShape = Shape.MakeDefault(channelSystem);
			shapes.Add(defaultShape);
		}
		
		return shapes;
	}

	public static Shape Load(IArchiveDirectory shapeDirectory) {
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
