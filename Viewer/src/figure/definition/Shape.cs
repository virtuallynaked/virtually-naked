using System.Collections.Generic;
using System.Linq;

public class Shape {
	public struct ParentOverride {
		public Channel channel;
		public double value;
	}

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
			var defaultShape = Shape.LoadDefault(channelSystem, figureDir);
			shapes.Add(defaultShape);
		}
		
		return shapes;
	}

	private static ChannelInputs LoadChannelInputs(ChannelSystem channelSystem, IArchiveFile channelInputsFile) {
		var shapeInputsByName = Persistance.Load<Dictionary<string, double>>(channelInputsFile);

		ChannelInputs channelInputs = channelSystem.MakeDefaultChannelInputs();
		foreach (var entry in shapeInputsByName) {
			Channel channel = channelSystem.ChannelsByName[entry.Key];
			channel.SetValue(channelInputs, entry.Value);
		}
		
		return channelInputs;
	}

	private static ParentOverride[] LoadParentOverrides(ChannelSystem channelSystem, IArchiveFile parentOverridesFile) {
		if (parentOverridesFile == null) {
			return null;
		}

		var parentChannelSystem = channelSystem.Parent;
		var parentOverridesByName = Persistance.Load<Dictionary<string, double>>(parentOverridesFile);
		var parentOverrides = parentOverridesByName
			.Select(entry => new ParentOverride {
					channel = parentChannelSystem.ChannelsByName[entry.Key],
					value = entry.Value
			})
			.ToArray();
		return parentOverrides;
	}

	public static Shape Load(ChannelSystem channelSystem, IArchiveDirectory shapeDirectory) {
		var channelInputsFile = shapeDirectory.File("channel-inputs.dat");
		var channelInputs = LoadChannelInputs(channelSystem, channelInputsFile);

		var parentOverridesFile = shapeDirectory.File("parent-overrides.dat");
		var parentOverrides = LoadParentOverrides(channelSystem, parentOverridesFile);
		return new Shape(shapeDirectory.Name, shapeDirectory, channelInputs, parentOverrides);
	}

	public static Shape LoadDefault(ChannelSystem channelSystem, IArchiveDirectory figureDirectory) {
		var channelInputsFile = figureDirectory.File("channel-inputs.dat");
		var channelInputs = channelInputsFile != null ?
			LoadChannelInputs(channelSystem, channelInputsFile) :
			channelSystem.MakeDefaultChannelInputs();

		var parentOverridesFile = figureDirectory.File("parent-overrides.dat");
		var parentOverrides = LoadParentOverrides(channelSystem, parentOverridesFile);

		return new Shape(DefaultLabel, null, channelInputs, parentOverrides);
	}

	public string Label { get; }
	public ChannelInputs ChannelInputs { get; }
	public ParentOverride[] ParentOverrides { get; }
	public IArchiveDirectory Directory { get; }

	public Shape(string label, IArchiveDirectory directory, ChannelInputs channelInputs, ParentOverride[] parentOverrides) {
		Label = label;
		Directory = directory;
		ChannelInputs = channelInputs;
		ParentOverrides = parentOverrides;
	}

	public void ApplyOverrides(ChannelInputs parentInputs) {
		if (ParentOverrides == null) {
			return;
		}

		foreach (var parentOverride in ParentOverrides) {
			parentOverride.channel.SetValue(parentInputs, parentOverride.value);
		}
	}
}
