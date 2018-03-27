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
		FigureDefinition definition = FigureDefinition.Load(dataDir, figureName, parentDefinition);

		InitialSettings.Shapes.TryGetValue(figureName, out string initialShapeName);
		InitialSettings.MaterialSets.TryGetValue(figureName, out string initialMaterialSetName);
		var model = new FigureModel(definition) {
			ShapeName = initialShapeName,
			MaterialSetName = initialMaterialSetName
		};
		
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, definition, model);
				
		var facade = new FigureFacade(device, shaderCache, definition, model, controlVertexProvider, figureRendererLoader);
		return facade;
	}
}
