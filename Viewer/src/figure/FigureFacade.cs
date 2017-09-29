using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureFacade : IDisposable {
	public static FigureFacade Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager, string figureName, FigureFacade parent) {
		IArchiveDirectory figureDir = dataDir.Subdirectory("figures").Subdirectory(figureName);
		
		
		FigureDefinition definition = FigureDefinition.Load(figureDir, parent?.definition);

		InitialSettings.Shapes.TryGetValue(figureName, out string initialShapeName);
		InitialSettings.MaterialSets.TryGetValue(figureName, out string initialMaterialSetName);
		var model = new FigureModel(definition) {
			ShapeName = initialShapeName,
			MaterialSetName = initialMaterialSetName
		};
		
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, definition, model);

		string materialSetName = model.MaterialSet.Label;
		var renderer = FigureRenderer.Load(figureDir, device, shaderCache, materialSetName);
		
		var facade = new FigureFacade(definition, model, controlVertexProvider, renderer);

		model.MaterialSetChanged += (oldMaterialSet, newMaterialSet) => {
			string newMaterialSetName = newMaterialSet.Label;
			var newRenderer = FigureRenderer.Load(figureDir, device, shaderCache, newMaterialSetName);
			facade.SetRenderer(newRenderer);
		};

		return facade;
	}
	
	private readonly FigureDefinition definition;
	private readonly FigureModel model;
	private readonly ControlVertexProvider controlVertexProvider;
	private FigureRenderer renderer;
	
	public IFigureAnimator Animator { get; set; } = null;

	public FigureFacade(FigureDefinition definition, FigureModel model, ControlVertexProvider controlVertexProvider, FigureRenderer renderer) {
		this.definition = definition;
		this.model = model;
		this.controlVertexProvider = controlVertexProvider;
		this.renderer = renderer;
	}
	
	public void Dispose() {
		controlVertexProvider.Dispose();
		renderer.Dispose();
	}

	public FigureDefinition Definition => definition;
	public FigureModel Model => model;
	public int VertexCount => controlVertexProvider.VertexCount;
	
	
	private void SetRenderer(FigureRenderer newRenderer) {
		renderer.Dispose();
		renderer = newRenderer;
	}
	
	public void RegisterChildren(List<FigureFacade> children) {
		var childControlVertexProviders = children
			.Select(child => child.controlVertexProvider)
			.ToList();

		controlVertexProvider.RegisterChildren(childControlVertexProviders);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		renderer.RenderPass(context, pass);
	}
	
	public ChannelOutputs UpdateFrame(FrameUpdateParameters updateParameters, ChannelOutputs parentOutputs) {
		var previousFrameResults = controlVertexProvider.GetPreviousFrameResults();

		ChannelInputs shapeInputs = model.Shape.ChannelInputs;
		ChannelInputs inputs = Animator != null ? Animator.GetFrameInputs(shapeInputs, updateParameters, previousFrameResults) : shapeInputs;
		
		return controlVertexProvider.UpdateFrame(parentOutputs, inputs);
	}

	public void UpdateVertexPositionsAndGetDeltas(UnorderedAccessView deltasOutView) {
		controlVertexProvider.UpdateVertexPositionsAndGetDeltas(deltasOutView);
	}

	public void UpdateVertexPositions(ShaderResourceView parentDeltasView) {
		controlVertexProvider.UpdateVertexPositions(parentDeltasView);
	}

	public void Update(DeviceContext context, ImageBasedLightingEnvironment lightingEnvironment) {
		renderer.Update(context, lightingEnvironment, controlVertexProvider.ControlVertexInfosView);
	}

	public class Recipe {
		[JsonProperty("shape")]
		public string shape;
		
		[JsonProperty("material-set")]
		public string materialSet;

		public void Merge(FigureFacade figure) {
			if (shape != null) {
				figure.Model.ShapeName = shape;
			}

			if (materialSet != null) {
				figure.Model.MaterialSetName = materialSet;
			}
		}
	}
	
	public Recipe Recipize() {
		return new Recipe {
			shape = model.ShapeName,
			materialSet = model.MaterialSetName
		};
	}
}
