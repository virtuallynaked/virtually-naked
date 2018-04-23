using System;
using System.Linq;
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

		var figureConfDir = pathManager.GetConfDirForFigure(figure.Name);
		MaterialSetImportConfiguration baseMaterialSetConfiguration = MaterialSetImportConfiguration.Load(figureConfDir).Single(conf => conf.name == "Base");
		ShapeImportConfiguration baseShapeImportConfiguration = ShapeImportConfiguration.Load(figureConfDir).SingleOrDefault(conf => conf.name == "Base");

		ShapeDumper shapeDumper = new ShapeDumper(fileLocator, device, shaderCache, pathManager, parentFigure, figure, baseShapeImportConfiguration);
		return new FigureDumper(fileLocator, objectLocator, pathManager, device, shaderCache, parentFigure, figure, baseMaterialSetConfiguration, baseShapeImportConfiguration, shapeDumper);
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
	private readonly MaterialSetImportConfiguration baseMaterialSetImportConfiguration;
	private readonly ShapeImportConfiguration baseShapeImportConfiguration;
	private readonly ShapeDumper shapeDumper;

	public FigureDumper(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, ImporterPathManager pathManager, Device device, ShaderCache shaderCache, Figure parentFigure, Figure figure, MaterialSetImportConfiguration baseMaterialSetImportConfiguration, ShapeImportConfiguration baseShapeImportConfiguration, ShapeDumper shapeDumper) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.pathManager = pathManager;
		this.parentFigure = parentFigure;
		this.figure = figure;
		this.baseMaterialSetImportConfiguration = baseMaterialSetImportConfiguration;
		this.baseShapeImportConfiguration = baseShapeImportConfiguration;
		this.shapeDumper = shapeDumper;
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

	public void DumpMaterialSet(ImportSettings importSettings, TextureProcessorSharer textureProcessorSharer, MaterialSetImportConfiguration conf) {
		var surfaceProperties = SurfacePropertiesJson.Load(pathManager, figure);
		TextureProcessor sharedTextureProcessor = surfaceProperties.ShareTextures != null ?
			textureProcessorSharer.GetSharedProcessor(surfaceProperties.ShareTextures) : null;
		
		MaterialSetDumper.DumpMaterialSetAndScattering(importSettings, device, shaderCache, fileLocator, objectLocator, pathManager, figure, baseMaterialSetImportConfiguration, conf, sharedTextureProcessor);

		if (conf.useCustomOcclusion) {
			shapeDumper.DumpOcclusionForMaterialSet(conf.name);
		}
	}

	public void DumpShape(ShapeImportConfiguration conf) {
		shapeDumper.Dump(conf);
	}

	public void DumpBaseShape() {
		shapeDumper.DumpUnmorphed();
	}
}
