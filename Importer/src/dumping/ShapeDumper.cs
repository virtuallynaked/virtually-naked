using System.IO;
using System.Linq;
using System;
using SharpDX.Direct3D11;
using System.Collections.Generic;

class ShapeDumper {
	public static void DumpAllForFigure(ImportSettings settings, ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure parentFigure, Figure figure) {
		ShapeImportConfiguration[] configurations = ShapeImportConfiguration.Load(figure.Name);
		var baseConf = configurations.SingleOrDefault(conf => conf.name == "Base");

		ShapeDumper dumper = new ShapeDumper(fileLocator, device, shaderCache, parentFigure, figure, baseConf);
		
		foreach (var conf in configurations) {
			if (!settings.ShouldImportShape(figure.Name, conf.name)) {
				continue;
			}

			dumper.Dump(conf);
		}

		dumper.DumpUnmorphed();
	}
	
	private readonly ContentFileLocator fileLocator;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly Figure parentFigure;
	private readonly Figure figure;
	private readonly ShapeImportConfiguration baseConfiguration;
	private readonly DirectoryInfo figureDirectory;
	
	public ShapeDumper(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure parentFigure, Figure figure, ShapeImportConfiguration baseConfiguration) {
		this.fileLocator = fileLocator;
		this.device = device;
		this.shaderCache = shaderCache;
		this.parentFigure = parentFigure;
		this.figure = figure;
		this.baseConfiguration = baseConfiguration;
		this.figureDirectory = CommonPaths.WorkDir.Subdirectory("figures").Subdirectory(figure.Name);
	}
	
	private DirectoryInfo GetShapeDirectory(string shapeName) {
		return figureDirectory.Subdirectory("shapes").Subdirectory(shapeName);
	}

	private ChannelInputs MakeShapeInputs(ShapeImportConfiguration shapeImportConfiguration) {
		ChannelInputs inputs = figure.MakeDefaultChannelInputs();
		
		if (baseConfiguration != null) {
			foreach (var entry in baseConfiguration.morphs) {
				string channelName = entry.Key + "?value";
				figure.ChannelsByName[channelName].SetValue(inputs, entry.Value);
			}
		}

		if (shapeImportConfiguration != null) {
			foreach (var entry in shapeImportConfiguration.morphs) {
				string channelName = entry.Key + "?value";
				figure.ChannelsByName[channelName].SetValue(inputs, entry.Value);
			}
		}
		
		return inputs;
	}
		
	public void Dump(ShapeImportConfiguration shapeImportConfiguration) {
		DirectoryInfo shapeDirectory = GetShapeDirectory(shapeImportConfiguration.name);
		
		//generate inputs
		var shapeInputs = MakeShapeInputs(shapeImportConfiguration);
				
		DumpInputs(shapeDirectory, shapeInputs);
		DumpParentOverrides(shapeDirectory, shapeImportConfiguration);

		if (figure == parentFigure) {
			DumpOccluderParameters(shapeDirectory, shapeInputs);
		} else {
			DumpSimpleOcclusion(shapeDirectory, shapeInputs);
		}
    }

	public void DumpUnmorphed() {
		DirectoryInfo directory = figureDirectory.Subdirectory("occlusion");
		var shapeInputs = figure.MakeDefaultChannelInputs();
		DumpSimpleOcclusion(directory, shapeInputs);

		if (baseConfiguration != null) {
			DumpInputs(figureDirectory, MakeShapeInputs(null));
			DumpParentOverrides(figureDirectory, baseConfiguration);
		}
	}

	private void DumpInputs(DirectoryInfo shapeDirectory, ChannelInputs shapeInputs) {
		FileInfo shapeFile = shapeDirectory.File("channel-inputs.dat");
		if (shapeFile.Exists) {
			return;
		}

		//persist
		Dictionary<string, double> shapeInputsByName = new Dictionary<string, double>();
		foreach (var channel in figure.Channels) {
			double defaultValue = channel.InitialValue;
			double value = channel.GetInputValue(shapeInputs);
			if (value != defaultValue) {
				shapeInputsByName.Add(channel.Name, value);
			}
		}
		
		shapeDirectory.CreateWithParents();
		Persistance.Save(shapeFile, shapeInputsByName);
	}

	private void DumpParentOverrides(DirectoryInfo shapeDirectory, ShapeImportConfiguration configuration) {
		if (configuration.parentOverrides.Count == 0) {
			return;
		}

		FileInfo parentOverridesFile = shapeDirectory.File("parent-overrides.dat");
		if (parentOverridesFile.Exists) {
			return;
		}

		//persist
		var parentChannelSystem = figure.Parent.ChannelSystem;
		var parentOverridesByName = configuration.parentOverrides
			.ToDictionary(entry => {
				//look up the channel to confirm it exists
				var channel = parentChannelSystem.ChannelsByName[entry.Key];
				return channel.Name;
			},
			entry => entry.Value);
		
		shapeDirectory.CreateWithParents();
		Persistance.Save(parentOverridesFile, parentOverridesByName);
	}

	private void DumpOccluderParameters(DirectoryInfo shapeDirectory, ChannelInputs shapeInputs) {
		FileInfo occluderParametersFile = shapeDirectory.File("occluder-parameters.dat");
		if (occluderParametersFile.Exists) {
			return;
		}

		Console.WriteLine("Dumping occlusion system...");
		
		OccluderParameters parameters;
		using (var calculator = new OccluderParametersCalculator(fileLocator, device, shaderCache, figure, shapeInputs)) {
			parameters = calculator.CalculateOccluderParameters();
		}

		//persist
		shapeDirectory.CreateWithParents();
		Persistance.Save(occluderParametersFile, parameters);
	}
	
	private void DumpSimpleOcclusion(DirectoryInfo shapeDirectory, ChannelInputs shapeInputs) {
		FileInfo occlusionInfosFile = shapeDirectory.File("occlusion-infos.array");
		FileInfo parentOcclusionInfosFile = shapeDirectory.File("parent-occlusion-infos.array");

		if (occlusionInfosFile.Exists) {
			return;
		}
		
		Console.WriteLine("Calculating occlusion...");
		
		FigureGroup figureGroup;
		if (figure == parentFigure) {
			figureGroup = new FigureGroup(figure);
		} else {
			figureGroup = new FigureGroup(parentFigure, figure);
		}
		
		var inputs = new ChannelInputsGroup(parentFigure.MakeDefaultChannelInputs(), new ChannelInputs[] { shapeInputs });
		var outputs = figureGroup.Evaluate(inputs);
		FigureOcclusionCalculator.Result occlusionResult;
		using (var occlusionCalculator = new FigureOcclusionCalculator(fileLocator, device, shaderCache, figureGroup)) {
			occlusionResult = occlusionCalculator.CalculateOcclusionInformation(outputs);
		}
		
		shapeDirectory.Create();
		if (figure == parentFigure) {
			occlusionInfosFile.WriteArray(OcclusionInfo.PackArray(occlusionResult.ParentOcclusion));
		} else {
			occlusionInfosFile.WriteArray(OcclusionInfo.PackArray(occlusionResult.ChildOcclusions[0]));
			parentOcclusionInfosFile.WriteArray(OcclusionInfo.PackArray(occlusionResult.ParentOcclusion));
		}
	}
}
