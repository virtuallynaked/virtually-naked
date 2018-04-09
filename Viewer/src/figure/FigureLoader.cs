using SharpDX.Direct3D11;

public class FigureLoader {
	private readonly IArchiveDirectory dataDir;
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly FigureRendererLoader figureRendererLoader;

	public FigureLoader(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, FigureRendererLoader figureRendererLoader) {
		this.dataDir = dataDir;
		this.device = device;
		this.shaderCache = shaderCache;
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
			ShapeName = recipe.shape,
			MaterialSetName = recipe.materialSet
		};
		
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, definition);
				
		var facade = new FigureFacade(device, shaderCache, definition, model, controlVertexProvider, figureRendererLoader);
		return facade;
	}
}
