using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureFacade : IDisposable {
	public static FigureFacade Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager, string figureName, FigureFacade parent) {
		IArchiveDirectory figureDir = dataDir.Subdirectory("figures").Subdirectory(figureName);
		
		FigureActiveSettings.Shapes.TryGetValue(figureName, out string activeShapeName);
		var model = FigureModel.Load(figureDir, activeShapeName, FigureActiveSettings.MaterialSets[figureName], parent?.model);
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, figureDir, model);

		string materialSetName = model.Materials.Active.Label;
		var renderer = FigureRenderer.Load(figureDir, device, shaderCache, materialSetName);
		
		var facade = new FigureFacade(model, controlVertexProvider, renderer);

		model.Materials.Changed += (oldMaterialSet, newMaterialSet) => {
			string newMaterialSetName = newMaterialSet.Label;
			var newRenderer = FigureRenderer.Load(figureDir, device, shaderCache, newMaterialSetName);
			facade.SetRenderer(newRenderer);
		};

		return facade;
	}
	
	private readonly FigureModel model;
	private readonly ControlVertexProvider controlVertexProvider;
	private FigureRenderer renderer;
	
	public IFigureAnimator Animator { get; set; } = null;

	public FigureFacade(FigureModel model, ControlVertexProvider controlVertexProvider, FigureRenderer renderer) {
		this.model = model;
		this.controlVertexProvider = controlVertexProvider;
		this.renderer = renderer;
	}
	
	public void Dispose() {
		controlVertexProvider.Dispose();
		renderer.Dispose();
	}
	
	private void SetRenderer(FigureRenderer newRenderer) {
		renderer.Dispose();
		renderer = newRenderer;
	}
	
	public int VertexCount => controlVertexProvider.VertexCount;
	
	public FigureModel Model => model;


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

		ChannelInputs shapeInputs = model.Shapes.Active.ChannelInputs;
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
				figure.model.Shapes.ActiveName = shape;
			}

			if (materialSet != null) {
				figure.model.Materials.ActiveName = materialSet;
			}
		}
	}
	
	public Recipe Recipize() {
		return new Recipe {
			shape = model.Shapes.ActiveName,
			materialSet = model.Materials.ActiveName
		};
	}
}
