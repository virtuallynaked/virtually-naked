using SharpDX.Direct3D11;

public class FigureLoader {
	private readonly IArchiveDirectory dataDir;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly ShapeNormalsLoader shapeNormalsLoader;
	private readonly FigureRendererLoader figureRendererLoader;

	public FigureLoader(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ShapeNormalsLoader shapeNormalsLoader, FigureRendererLoader figureRendererLoader) {
		this.dataDir = dataDir;
		this.device = device;
		this.shaderCache = shaderCache;
		this.shapeNormalsLoader = shapeNormalsLoader;
		this.figureRendererLoader = figureRendererLoader;
	}

	public FigureFacade Load(string figureName, FigureDefinition parentDefinition) {
		InitialSettings.Shapes.TryGetValue(figureName, out string initialShapeName);
		InitialSettings.MaterialSets.TryGetValue(figureName, out string initialMaterialSetName);

		var recipe = new FigureFacade.Recipe {
			name = figureName,
			isVisible = true,
			shape = initialShapeName,
			materialSet = initialMaterialSetName
		};

		return Load(recipe, parentDefinition);
	}

	public FigureFacade Load(FigureFacade.Recipe recipe, FigureDefinition parentDefinition) {
		FigureDefinition definition = FigureDefinition.Load(dataDir, recipe.name, parentDefinition);

		var model = new FigureModel(definition) {
			IsVisible = recipe.isVisible,
			ShapeName = recipe.shape
		};
		model.SetMaterialSetAndVariantByName(recipe.materialSet, recipe.materialVariants);
		
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, definition);
				
		var facade = new FigureFacade(device, shaderCache, definition, model, controlVertexProvider, shapeNormalsLoader, figureRendererLoader);
		return facade;
	}
}
