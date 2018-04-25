using System;
using System.IO;
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
	private readonly float[] parentFaceTransparencies;

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
		SurfaceProperties genesis3FemaleSurfaceProperties = SurfacePropertiesJson.Load(pathManager, genesis3FemaleWithGenitalia);
		float[] genesis3FemaleFaceTransparencies = FaceTransparencies.For(genesis3FemaleWithGenitalia, genesis3FemaleSurfaceProperties, null);

		parentFigureRecipe = genesis3FemaleRecipe;
		parentFigure = genesis3FemaleWithGenitalia;
		parentFaceTransparencies = genesis3FemaleFaceTransparencies;
	}

	public FigureDumper LoadDumper(string figureName) {
		var figure = figureName == parentFigure.Name ?
			parentFigure :
			figureRecipeLoader.LoadFigureRecipe(figureName, parentFigureRecipe).Bake(parentFigure);

		var figureConfDir = pathManager.GetConfDirForFigure(figure.Name);
		MaterialSetImportConfiguration baseMaterialSetConfiguration = MaterialSetImportConfiguration.Load(figureConfDir).Single(conf => conf.name == "Base");
		ShapeImportConfiguration baseShapeImportConfiguration = ShapeImportConfiguration.Load(figureConfDir).SingleOrDefault(conf => conf.name == "Base");
		SurfaceProperties surfaceProperties = SurfacePropertiesJson.Load(pathManager, figure);

		ShapeDumper shapeDumper = new ShapeDumper(fileLocator, device, shaderCache, parentFigure, parentFaceTransparencies, figure, surfaceProperties, baseShapeImportConfiguration);
		return new FigureDumper(fileLocator, objectLocator, device, shaderCache, parentFigure, figure, surfaceProperties, baseMaterialSetConfiguration, baseShapeImportConfiguration, shapeDumper);
	}
}

public class FigureDumper {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly Figure parentFigure;
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly MaterialSetImportConfiguration baseMaterialSetImportConfiguration;
	private readonly ShapeImportConfiguration baseShapeImportConfiguration;
	private readonly ShapeDumper shapeDumper;

	public FigureDumper(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Device device, ShaderCache shaderCache, Figure parentFigure, Figure figure, SurfaceProperties surfaceProperties, MaterialSetImportConfiguration baseMaterialSetImportConfiguration, ShapeImportConfiguration baseShapeImportConfiguration, ShapeDumper shapeDumper) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.parentFigure = parentFigure;
		this.figure = figure;
		this.surfaceProperties =surfaceProperties;
		this.baseMaterialSetImportConfiguration = baseMaterialSetImportConfiguration;
		this.baseShapeImportConfiguration = baseShapeImportConfiguration;
		this.shapeDumper = shapeDumper;
	}

	public void DumpFigure(ShapeImportConfiguration[] shapeConfigurations, DirectoryInfo figureDestDir) {
		Console.WriteLine($"Dumping {figure.Name}...");
		
		if (figure == parentFigure) {
			AnimationDumper.DumpAllAnimations(parentFigure, figureDestDir);
		}
		
		bool[] channelsToInclude = figure != parentFigure ? ChannelShaker.MakeChannelsToIncludeFromShapes(figure, shapeConfigurations) : null;
		SystemDumper.DumpFigure(figure, surfaceProperties, channelsToInclude, figureDestDir);
		GeometryDumper.DumpFigure(figure, surfaceProperties, figureDestDir);
		UVSetDumper.DumpFigure(figure, surfaceProperties, figureDestDir);
	}

	public void DumpMaterialSet(ImportSettings importSettings, TextureProcessor textureProcessor, DirectoryInfo figureDestDir, MaterialSetImportConfiguration conf) {
		MaterialSetDumper.DumpMaterialSetAndScattering(importSettings, device, shaderCache, fileLocator, objectLocator, figure, surfaceProperties, baseMaterialSetImportConfiguration, textureProcessor, figureDestDir, conf);

		if (conf.useCustomOcclusion) {
			shapeDumper.DumpOcclusionForMaterialSet(figureDestDir, conf.name);
		}
	}

	public void DumpShape(DirectoryInfo figureDestDir, ShapeImportConfiguration conf) {
		shapeDumper.DumpShape(figureDestDir, conf);
	}

	public void DumpBaseShape(DirectoryInfo figureDestDir) {
		shapeDumper.DumpUnmorphed(figureDestDir);
	}
}
