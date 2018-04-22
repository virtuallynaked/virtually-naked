using System;
using SharpDX.Direct3D11;

public class FigureDumperLoader {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly ImporterPathManager pathManager;
	private readonly Device device;
	private readonly ShaderCache shaderCache;

	private readonly FigureRecipeLoader figureRecipeLoader;
	private readonly FigureRecipe parentFigureRecipe;
	private readonly Figure parentFigure;

	public FigureDumperLoader(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, ImporterPathManager pathManager, Device device, ShaderCache shaderCache) {
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.pathManager = pathManager;
		this.device = device;
		this.shaderCache = shaderCache;

		figureRecipeLoader = new FigureRecipeLoader(objectLocator, pathManager);

		FigureRecipe genesis3FemaleRecipe = figureRecipeLoader.LoadFigureRecipe("genesis-3-female", null);
		FigureRecipe genitaliaRecipe = figureRecipeLoader.LoadFigureRecipe("genesis-3-female-genitalia", genesis3FemaleRecipe);
		FigureRecipe genesis3FemaleWithGenitaliaRecipe = new FigureRecipeMerger(genesis3FemaleRecipe, genitaliaRecipe).Merge();
		Figure genesis3FemaleWithGenitalia = genesis3FemaleWithGenitaliaRecipe.Bake(null);

		parentFigureRecipe = genesis3FemaleRecipe;
		parentFigure = genesis3FemaleWithGenitalia;
	}

	public FigureDumper LoadDumper(string figureName) {
		var figure = figureName == parentFigure.Name ?
			parentFigure :
			figureRecipeLoader.LoadFigureRecipe(figureName, parentFigureRecipe).Bake(parentFigure);

		return new FigureDumper(fileLocator, objectLocator, pathManager, device, shaderCache, parentFigure, figure);
	}
}

public class FigureDumper {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly ImporterPathManager pathManager;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly Figure parentFigure;
	private readonly Figure figure;

	public FigureDumper(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, ImporterPathManager pathManager, Device device, ShaderCache shaderCache, Figure parentFigure, Figure figure) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.pathManager = pathManager;
		this.parentFigure = parentFigure;
		this.figure = figure;
	}

	public void DumpFigure() {
		Console.WriteLine($"Dumping {figure.Name}...");

		bool[] channelsToInclude = figure != parentFigure ? ChannelShaker.MakeChannelsToIncludeFromShapes(pathManager, figure) : null;

		if (figure == parentFigure) {
			AnimationDumper.DumpAllAnimations(pathManager, parentFigure);
		}
				
		SystemDumper.DumpFigure(pathManager, figure, channelsToInclude);
		GeometryDumper.DumpFigure(pathManager, figure);
		UVSetDumper.DumpFigure(pathManager, figure);
	}

	public void DumpMaterialSets(ImportSettings importSettings, TextureProcessorSharer textureProcessorSharer) {
		MaterialSetDumper.DumpAllForFigure(importSettings, device, shaderCache, fileLocator, objectLocator, pathManager, figure, textureProcessorSharer);
	}

	public void DumperShapes(ImportSettings importSettings) {
		ShapeDumper.DumpAllForFigure(importSettings, fileLocator, device, shaderCache, pathManager, parentFigure, figure);
	}
}
