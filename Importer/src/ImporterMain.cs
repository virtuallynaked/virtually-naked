using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

public class ImporterMain : IDisposable {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly Device device;
	private readonly ShaderCache shaderCache;

	private static ImporterMain Make(string[] args) {
		ContentFileLocator fileLocator = new ContentFileLocator();
		DsonObjectLocator objectLocator = new DsonObjectLocator(fileLocator);
		return new ImporterMain(fileLocator, objectLocator);
	}

	public static void Main(string[] args) {
		using (var app = Make(args)) {
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
			app.Run(args);
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
		}
	}

	public ImporterMain(ContentFileLocator fileLocator, DsonObjectLocator objectLocator) {
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.device = new Device(DriverType.Hardware, DeviceCreationFlags.None, FeatureLevel.Level_11_1);
		this.shaderCache = new ShaderCache(device);
	}
	
	public void Dispose() {
		device.Dispose();
		shaderCache.Dispose();
	}
			
	private void Run(string[] args) {
		ImportSettings settings;
		if (args.Length > 0 && args[0] == "release") {
			settings = ImportSettings.MakeReleaseSettings();
		} else {
			settings = ImportSettings.MakeFromViewerInitialSettings();
		}
		
		var contentDestDir = CommonPaths.WorkDir.Subdirectory("content");

		var contentPackConfs = ContentPackImportConfiguration.LoadAll(CommonPaths.ConfDir);
		var pathManager = ImporterPathManager.Make(contentPackConfs);
		
		var figureDumperLoader = new FigureDumperLoader(fileLocator, objectLocator, pathManager, device, shaderCache);

		foreach (var contentPackConf in contentPackConfs) {
			var destDir = contentDestDir.Subdirectory(contentPackConf.Name);

			if (contentPackConf.IsCore) {
				new UiImporter(destDir).Run();
				new EnvironmentCubeGenerator().Run(settings, destDir);
			}
		
			foreach (var outfitConf in contentPackConf.Outfits) {
				OutfitImporter.Import(pathManager, outfitConf.File, destDir);
			}

			var texturesDir = destDir.Subdirectory("textures").Subdirectory(contentPackConf.Name);
			var textureProcessor = new TextureProcessor(device, shaderCache, texturesDir, contentPackConf.Name, settings.CompressTextures);

			foreach (var figureConf in contentPackConf.Figures) {
				string figureName = figureConf.Name;

				if (!settings.FiguresToImport.Contains(figureName)) {
					continue;
				}

				var figureDumper = figureDumperLoader.LoadDumper(figureName);
				
				MaterialSetImportConfiguration[] materialSetConfigurations = MaterialSetImportConfiguration.Load(figureConf.Directory);
				ShapeImportConfiguration[] shapeImportConfigurations = ShapeImportConfiguration.Load(figureConf.Directory);

				var figureDestDir = destDir.Subdirectory("figures").Subdirectory(figureName);

				if (figureConf.IsPrimary) {
					figureDumper.DumpFigure(shapeImportConfigurations, figureDestDir);
				}
				
				foreach (var materialSetConf in materialSetConfigurations) {
					if (!settings.ShouldImportMaterialSet(figureConf.Name, materialSetConf.name)) {
						continue;
					}

					figureDumper.DumpMaterialSet(settings, textureProcessor, figureDestDir, materialSetConf);
				}

				if (figureConf.IsPrimary) {
					figureDumper.DumpBaseShape(figureDestDir);
				}

				foreach (var shapeConf in shapeImportConfigurations) {
					if (!settings.ShouldImportShape(figureConf.Name, shapeConf.name)) {
						continue;
					}

					figureDumper.DumpShape(figureDestDir, shapeConf);

				}
			}

			textureProcessor.ImportAll();
		}
	}
}
