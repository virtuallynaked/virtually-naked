using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class MaterialSetDumper {
	private static MultiMaterialSettings DumpMaterialSet(Device device, ShaderCache shaderCache, ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Figure figure, MaterialSetImportConfiguration baseConfiguration, MaterialSetImportConfiguration configuration) {
		DirectoryInfo figuresDirectory = CommonPaths.WorkDir.Subdirectory("figures");
		DirectoryInfo figureDirectory = figuresDirectory.Subdirectory(figure.Name);
		DirectoryInfo texturesDirectory = figureDirectory.Subdirectory("textures");
		DirectoryInfo materialsSetsDirectory = figureDirectory.Subdirectory("material-sets");
		DirectoryInfo materialSetDirectory = materialsSetsDirectory.Subdirectory(configuration.name);
		FileInfo materialSettingsFileInfo = materialSetDirectory.File("material-settings.dat");
		if (materialSettingsFileInfo.Exists) {
			return Persistance.Load<MultiMaterialSettings>(UnpackedArchiveFile.Make(materialSettingsFileInfo));
		}
		
		var aggregator = new DsonMaterialAggregator(fileLocator, objectLocator);
		IEnumerable<string> dufPaths = Enumerable.Concat(baseConfiguration.materialsDufPaths, configuration.materialsDufPaths);
		foreach (string path in dufPaths) {
			DsonTypes.DsonDocument doc = objectLocator.LocateRoot(path);
			aggregator.IncludeDuf(doc.Root);
		}
		
		TextureProcessor textureProcessor;
		IMaterialImporter materialImporter;
		if (figure.Name.EndsWith("-hair")) {
			textureProcessor = new TextureProcessor(device, shaderCache, texturesDirectory);
			materialImporter = new HairMaterialImporter(figure, textureProcessor);
		} else {
			textureProcessor = new TextureProcessor(device, shaderCache, materialSetDirectory);
			materialImporter = new UberMaterialImporter(figure, textureProcessor);
		}
		
		var perMaterialSettings = Enumerable.Range(0, figure.Geometry.SurfaceCount)
			.Select(surfaceIdx => {
				string surfaceName = figure.Geometry.SurfaceNames[surfaceIdx];
				var bag = aggregator.GetBag(surfaceName);
				var materialSettings = materialImporter.Import(surfaceIdx, bag);
				return materialSettings;
			})
			.ToArray();

		var multiMaterialSettings = new MultiMaterialSettings(perMaterialSettings);
		
		materialSetDirectory.CreateWithParents();
		textureProcessor.ImportAll();
		Persistance.Save(materialSettingsFileInfo, multiMaterialSettings);

		return multiMaterialSettings;
	}

	public static void DumpMaterialSetAndScattering(Device device, ShaderCache shaderCache, ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Figure figure,
		MaterialSetImportConfiguration baseConfiguration, MaterialSetImportConfiguration configuration) {
		var materialSettings = DumpMaterialSet(device, shaderCache, fileLocator, objectLocator, figure, baseConfiguration, configuration);
		ScatteringDumper.Dump(figure, materialSettings.PerMaterialSettings, configuration.name);
	}

	public static void DumpAllForFigure(Device device, ShaderCache shaderCache, ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Figure figure) {
		MaterialSetImportConfiguration[] configurations = MaterialSetImportConfiguration.Load(figure.Name);

		var baseConf = configurations.Single(conf => conf.name == "Base");

		FigureActiveSettings.MaterialSets.TryGetValue(figure.Name, out string activeConfiguration);

		foreach (var conf in configurations) {
			if (conf == baseConf) {
				continue;
			}

			if (conf.name != activeConfiguration) {
				continue;
			}

			DumpMaterialSetAndScattering(device, shaderCache, fileLocator, objectLocator, figure, baseConf, conf);
		}
	}
}