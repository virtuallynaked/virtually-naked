using System.IO;
using System.Linq;
using System;
using SharpDX.Direct3D11;
using System.Collections.Generic;

public class ShapeDumper {
	private readonly ContentFileLocator fileLocator;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly Figure parentFigure;
	private readonly float[] parentFaceTransparencies;
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly ShapeImportConfiguration baseConfiguration;
	private readonly HdMorphToNormalMapConverter hdMorphToNormalMapConverter;

	public ShapeDumper(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure parentFigure, float[] parentFaceTransparencies, Figure figure, SurfaceProperties surfaceProperties, ShapeImportConfiguration baseConfiguration, HdMorphToNormalMapConverter hdMorphToNormalMapConverter) {
		this.fileLocator = fileLocator;
		this.device = device;
		this.shaderCache = shaderCache;
		this.parentFigure = parentFigure;
		this.parentFaceTransparencies = parentFaceTransparencies;
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
		this.baseConfiguration = baseConfiguration;
		this.hdMorphToNormalMapConverter = hdMorphToNormalMapConverter;
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
		
	public void DumpShape(TextureProcessor textureProcessor, DirectoryInfo figureDestDir, ShapeImportConfiguration shapeImportConfiguration) {
		DirectoryInfo shapeDirectory = figureDestDir.Subdirectory("shapes").Subdirectory(shapeImportConfiguration.name);
		
		//generate inputs
		var shapeInputs = MakeShapeInputs(shapeImportConfiguration);

		DumpNormals(textureProcessor, shapeDirectory, shapeImportConfiguration, shapeInputs);

		DumpInputs(shapeDirectory, shapeInputs);
		DumpParentOverrides(shapeDirectory, shapeImportConfiguration);

		var faceTransparencies = FaceTransparencies.For(figure, surfaceProperties, figureDestDir);

		if (figure == parentFigure) {
			DumpOccluderParameters(shapeDirectory, shapeInputs, faceTransparencies);
		} else {
			DumpSimpleOcclusion(shapeDirectory, shapeInputs, faceTransparencies);
		}
    }

	private void DumpNormals(TextureProcessor textureProcessor, DirectoryInfo shapeDirectory, ShapeImportConfiguration shapeImportConfiguration, ChannelInputs shapeInputs) {
		var normalsConf = shapeImportConfiguration?.normals;
		var baseNormalsConf = baseConfiguration?.normals;

		if (normalsConf == null && baseNormalsConf == null) {
			return;
		}
				
		var recipeFile = shapeDirectory.File("shape-normals.dat");
		if (recipeFile.Exists) {
			return;
		}

		var surfaceGroups = normalsConf?.surfaceGroups ?? baseNormalsConf?.surfaceGroups;

		var uvSetName = normalsConf?.uvSet ?? baseNormalsConf?.uvSet ?? figure.DefaultUvSet.Name;
		var uvSet = figure.UvSets[uvSetName];
		
		var surfaceNames = figure.Geometry.SurfaceNames;
		Dictionary<string, int> surfaceNameToIdx = Enumerable.Range(0, surfaceNames.Length)
			.ToDictionary(idx => surfaceNames[idx], idx => idx);

		bool generateFromHd = normalsConf?.generatedFromHd ?? false;
		var generatedTextureDirectory = CommonPaths.WorkDir.Subdirectory("generated-textures");
		NormalMapRenderer normalMapRenderer;
		if (generateFromHd) {
			Console.WriteLine($"Generating normals for shape '{shapeImportConfiguration.name}'...");
			normalMapRenderer = hdMorphToNormalMapConverter.MakeNormalMapRenderer(figure, uvSet, shapeInputs);
		} else {
			normalMapRenderer = null;
		}
		
		string[] textureNamesBySurface = Enumerable.Repeat(ShapeNormalsRecipe.DefaultTextureName, surfaceNames.Length).ToArray();
		
		for (int groupIdx = 0; groupIdx < surfaceGroups.Count; ++groupIdx) {
			var surfaceIdxs = surfaceGroups[groupIdx]
				.Select(surfaceName => surfaceNameToIdx[surfaceName])
				.ToList();

			FileInfo textureFile;
			if (normalMapRenderer == null) {
				var texturePath = normalsConf?.textures?[groupIdx];
				if (texturePath == null) {
					continue;
				}

				textureFile = fileLocator.Locate(texturePath).File;
			} else {
				textureFile = generatedTextureDirectory.File($"normal-map-{shapeImportConfiguration.name}-{groupIdx}.png");
				if (!textureFile.Exists) {
					var normalMap = normalMapRenderer.Render(new HashSet<int>(surfaceIdxs));
					generatedTextureDirectory.CreateWithParents();
					normalMap.Save(textureFile);
					normalMap.Dispose();
				}
			}

			foreach (int surfaceIdx in surfaceIdxs) {
				var mask = TextureMask.Make(uvSet, figure.Geometry.SurfaceMap, surfaceIdx);
				var textureName = textureProcessor.RegisterForProcessing(textureFile, TextureProcessingType.Normal, true, mask);
				textureNamesBySurface[surfaceIdx] = textureName;
			}
		}

		normalMapRenderer?.Dispose();
		
		var recipe = new ShapeNormalsRecipe(uvSetName, textureNamesBySurface);

		textureProcessor.RegisterAction(() => {
			shapeDirectory.CreateWithParents();
			Persistance.Save(recipeFile, recipe);
		});
	}

	public void DumpUnmorphed(DirectoryInfo figureDestDir) {
		var shapeInputs = figure.MakeDefaultChannelInputs();

		if (baseConfiguration != null) {
			DumpInputs(figureDestDir, MakeShapeInputs(null));
			DumpParentOverrides(figureDestDir, baseConfiguration);
		}

		DirectoryInfo occlusionDirectory = figureDestDir.Subdirectory("occlusion");
		var faceTransparencies = FaceTransparencies.For(figure, surfaceProperties, figureDestDir);
		DumpSimpleOcclusion(occlusionDirectory, shapeInputs, faceTransparencies);
	}

	public void DumpOcclusionForMaterialSet(DirectoryInfo figureDestDir, string materialSetName) {
		DirectoryInfo directory = figureDestDir.Subdirectory("material-sets").Subdirectory(materialSetName);
		float[] faceTransparencies = directory.File("face-transparencies.array").ReadArray<float>();
		var shapeInputs = figure.MakeDefaultChannelInputs();
		DumpSimpleOcclusion(directory, shapeInputs, faceTransparencies);
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

	private void DumpOccluderParameters(DirectoryInfo shapeDirectory, ChannelInputs shapeInputs, float[] faceTransparencies) {
		FileInfo occluderParametersFile = shapeDirectory.File("occluder-parameters.dat");
		if (occluderParametersFile.Exists) {
			return;
		}

		Console.WriteLine("Dumping occlusion system...");
		
		OccluderParameters parameters;
		using (var calculator = new OccluderParametersCalculator(fileLocator, device, shaderCache, figure, faceTransparencies, shapeInputs)) {
			parameters = calculator.CalculateOccluderParameters();
		}

		//persist
		shapeDirectory.CreateWithParents();
		Persistance.Save(occluderParametersFile, parameters);
	}
	
	private void DumpSimpleOcclusion(DirectoryInfo shapeDirectory, ChannelInputs shapeInputs, float[] faceTransparencies) {
		FileInfo occlusionInfosFile = shapeDirectory.File("occlusion-infos.array");
		FileInfo parentOcclusionInfosFile = shapeDirectory.File("parent-occlusion-infos.array");

		if (occlusionInfosFile.Exists) {
			return;
		}
		
		Console.WriteLine("Calculating occlusion...");
		
		FigureGroup figureGroup;
		FaceTransparenciesGroup faceTransparenciesGroup;
		if (figure == parentFigure) {
			figureGroup = new FigureGroup(figure);
			faceTransparenciesGroup = new FaceTransparenciesGroup(faceTransparencies);
		} else {
			figureGroup = new FigureGroup(parentFigure, figure);
			faceTransparenciesGroup = new FaceTransparenciesGroup(parentFaceTransparencies, faceTransparencies);
		}

		var inputs = new ChannelInputsGroup(parentFigure.MakeDefaultChannelInputs(), new ChannelInputs[] { shapeInputs });
		var outputs = figureGroup.Evaluate(inputs);
		FigureOcclusionCalculator.Result occlusionResult;
		using (var occlusionCalculator = new FigureOcclusionCalculator(fileLocator, device, shaderCache, figureGroup, faceTransparenciesGroup)) {
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
