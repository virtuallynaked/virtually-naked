using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
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
		
		var contentPackConfs = ContentPackImportConfiguration.LoadAll(CommonPaths.ConfDir);
		var pathManager = ImporterPathManager.Make(contentPackConfs);
		
		var loader = new FigureRecipeLoader(objectLocator, pathManager);

		FigureRecipe genesis3FemaleRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		FigureRecipe genitaliaRecipe = loader.LoadFigureRecipe("genesis-3-female-genitalia", genesis3FemaleRecipe);
		FigureRecipe genesis3FemaleWithGenitaliaRecipe = new FigureRecipeMerger(genesis3FemaleRecipe, genitaliaRecipe).Merge();
		Figure genesis3FemaleWithGenitalia = genesis3FemaleWithGenitaliaRecipe.Bake(null);

		Figure parentFigure = genesis3FemaleWithGenitalia;
		
		foreach (var contentPackConf in contentPackConfs) {
			var destDir = pathManager.GetDestDirForContentPack(contentPackConf.Name);

			if (contentPackConf.IsCore) {
				new UiImporter(destDir).Run();
				new EnvironmentCubeGenerator().Run(settings, destDir);
			}
		
			foreach (var outfitConf in contentPackConf.Outfits) {
				OutfitImporter.Import(pathManager, outfitConf.File, destDir);
			}

			var textureProcessorSharer = new TextureProcessorSharer(device, shaderCache, settings.CompressTextures, destDir);

			foreach (var figureConf in contentPackConf.Figures) {
				if (!settings.FiguresToImport.Contains(figureConf.Name)) {
					continue;
				}

				var figure = figureConf.Name == parentFigure.Name ?
					parentFigure :
					loader.LoadFigureRecipe(figureConf.Name, genesis3FemaleRecipe).Bake(parentFigure);

				bool[] channelsToInclude = figure != parentFigure ? ChannelShaker.MakeChannelsToIncludeFromShapes(pathManager, figure) : null;

				Console.WriteLine($"Dumping {figure.Name}...");

				if (figure == parentFigure) {
					AnimationDumper.DumpAllAnimations(pathManager, parentFigure);
				}
				
				SystemDumper.DumpFigure(pathManager, figure, channelsToInclude);
				GeometryDumper.DumpFigure(pathManager, figure);
				UVSetDumper.DumpFigure(pathManager, figure);

				MaterialSetDumper.DumpAllForFigure(settings, device, shaderCache, fileLocator, objectLocator, pathManager, figure, textureProcessorSharer);
			
				ShapeDumper.DumpAllForFigure(settings, fileLocator, device, shaderCache, pathManager, parentFigure, figure);
			}

			textureProcessorSharer.Finish();
		}
	}
}
