using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
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

		new UiImporter().Run();
		new EnvironmentCubeGenerator().Run(settings);
		
		OutfitImporter.ImportAll();

		var loader = new FigureRecipeLoader(objectLocator);

		FigureRecipe genesis3FemaleRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		FigureRecipe genitaliaRecipe = loader.LoadFigureRecipe("genesis-3-female-genitalia", genesis3FemaleRecipe);
		FigureRecipe genesis3FemaleWithGenitaliaRecipe = new FigureRecipeMerger(genesis3FemaleRecipe, genitaliaRecipe).Merge();
		Figure genesis3FemaleWithGenitalia = genesis3FemaleWithGenitaliaRecipe.Bake(null);

		Figure parentFigure = genesis3FemaleWithGenitalia;
		
		List<Figure> childFigures = settings.FiguresToImport
			.Where(figureName => figureName != parentFigure.Name)
			.Select(figureName => loader.LoadFigureRecipe(figureName, genesis3FemaleRecipe).Bake(parentFigure))
			.ToList();

		List<Figure> figuresToDump = Enumerable.Repeat(parentFigure, 1)
			.Concat(childFigures)
			.ToList();

		Console.WriteLine($"Dumping parent...");
		AnimationDumper.DumpAllAnimations(parentFigure);

		var textureProcessorSharer = new TextureProcessorSharer(device, shaderCache, settings.CompressTextures);

		foreach (Figure figure in figuresToDump) {
			bool[] channelsToInclude = figure != parentFigure ? ChannelShaker.MakeChannelsToIncludeFromShapes(figure) : null;

			Console.WriteLine($"Dumping {figure.Name}...");
			SystemDumper.DumpFigure(figure, channelsToInclude);
			GeometryDumper.DumpFigure(figure);
			UVSetDumper.DumpFigure(figure);

			MaterialSetDumper.DumpAllForFigure(settings, device, shaderCache, fileLocator, objectLocator, figure, textureProcessorSharer);
			
			ShapeDumper.DumpAllForFigure(settings, fileLocator, device, shaderCache, parentFigure, figure);
		}

		textureProcessorSharer.Finish();
	}
}
