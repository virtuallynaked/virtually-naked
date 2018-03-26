using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureFacade : IDisposable {
	public static FigureFacade Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, string figureName, FigureFacade parent) {
		FigureDefinition definition = FigureDefinition.Load(dataDir, figureName, parent?.definition);

		InitialSettings.Shapes.TryGetValue(figureName, out string initialShapeName);
		InitialSettings.MaterialSets.TryGetValue(figureName, out string initialMaterialSetName);
		var model = new FigureModel(definition) {
			ShapeName = initialShapeName,
			MaterialSetName = initialMaterialSetName
		};
		
		var controlVertexProvider = ControlVertexProvider.Load(device, shaderCache, definition, model);

		string materialSetName = model.MaterialSet.Label;
		var renderer = FigureRenderer.Load(definition.Directory, device, shaderCache, materialSetName);
		
		var facade = new FigureFacade(device, shaderCache, definition, model, controlVertexProvider, renderer);

		model.MaterialSetChanged += (oldMaterialSet, newMaterialSet) => {
			string newMaterialSetName = newMaterialSet.Label;
			var newRenderer = FigureRenderer.Load(definition.Directory, device, shaderCache, newMaterialSetName);
			facade.SetRenderer(newRenderer);
		};

		return facade;
	}
	
	private readonly FigureDefinition definition;
	private readonly FigureModel model;
	private readonly ControlVertexProvider controlVertexProvider;
	private FigureRenderer renderer;
	
	public IFigureAnimator Animator { get; set; } = null;

	public FigureFacade(Device device, ShaderCache shaderCache, FigureDefinition definition, FigureModel model, ControlVertexProvider controlVertexProvider, FigureRenderer renderer) {
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

	public void ReadbackPosedControlVertices(DeviceContext context) {
		controlVertexProvider.ReadbackPosedControlVertices(context);
	}

	public void RegisterChildren(List<FigureFacade> children) {
		var childControlVertexProviders = children
			.Select(child => child.controlVertexProvider)
			.ToList();

		controlVertexProvider.RegisterChildren(childControlVertexProviders);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		if (!model.IsVisible) {
			return;
		}

		renderer.RenderPass(context, pass);
	}
	
	public ChannelOutputs UpdateFrame(DeviceContext context, FrameUpdateParameters updateParameters, ChannelOutputs parentOutputs) {
		var previousFrameResults = controlVertexProvider.GetPreviousFrameResults(context);

		ChannelInputs shapeInputs = model.Shape.ChannelInputs;
		ChannelInputs inputs = Animator != null ? Animator.GetFrameInputs(shapeInputs, updateParameters, previousFrameResults) : shapeInputs;
		
		return controlVertexProvider.UpdateFrame(context, parentOutputs, inputs);
	}

	public void UpdateVertexPositionsAndGetDeltas(DeviceContext context, UnorderedAccessView deltasOutView) {
		controlVertexProvider.UpdateVertexPositionsAndGetDeltas(context, deltasOutView);
	}

	public void UpdateVertexPositions(DeviceContext context, ShaderResourceView parentDeltasView) {
		controlVertexProvider.UpdateVertexPositions(context, parentDeltasView);
	}

	public void Update(DeviceContext context, ImageBasedLightingEnvironment lightingEnvironment) {
		if (!model.IsVisible) {
			return;
		}

		renderer.Update(context, lightingEnvironment, controlVertexProvider.ControlVertexInfosView);
	}

	public class Recipe {
		[JsonProperty("name")]
		public string name;

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
			name = definition.Name,
			shape = model.ShapeName,
			materialSet = model.MaterialSetName
		};
	}
}
