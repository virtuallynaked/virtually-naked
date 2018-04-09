using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class FigureFacade : IDisposable {
	private readonly FigureDefinition definition;
	private readonly FigureModel model;
	private readonly ControlVertexProvider controlVertexProvider;
	private readonly FigureRendererLoader figureRendererLoader;

	private FigureRenderer renderer;
	private List<FigureFacade> children = new List<FigureFacade>();

	public IFigureAnimator Animator { get; set; } = null;

	public FigureFacade(Device device, ShaderCache shaderCache, FigureDefinition definition, FigureModel model, ControlVertexProvider controlVertexProvider, FigureRendererLoader figureRendererLoader) {
		this.definition = definition;
		this.model = model;
		this.controlVertexProvider = controlVertexProvider;
		this.figureRendererLoader = figureRendererLoader;

		model.MaterialSetChanged += Model_MaterialSetChanged;
		SyncMaterialSet();
	}
	
	public void Dispose() {
		model.MaterialSetChanged -= Model_MaterialSetChanged;
		controlVertexProvider.Dispose();
		renderer.Dispose();
	}

	public FigureDefinition Definition => definition;
	public FigureModel Model => model;
	public int VertexCount => controlVertexProvider.VertexCount;
	
	private void Model_MaterialSetChanged(MaterialSetOption oldMaterialSet, MaterialSetOption newMaterialSet) {
		SyncMaterialSet();
	}

	private void SyncMaterialSet() {
		string materialSetName = model.MaterialSet.Label;
		var newRenderer = figureRendererLoader.Load(definition.Directory, materialSetName);
		renderer?.Dispose();
		renderer = newRenderer;
	}

	public void SyncWithModel() {
		var childControlVertexProviders = children
			.Select(child => child.controlVertexProvider)
			.ToList();
		controlVertexProvider.SyncWithModel(model, childControlVertexProviders);
	}

	public void ReadbackPosedControlVertices(DeviceContext context) {
		controlVertexProvider.ReadbackPosedControlVertices(context);
	}

	public void RegisterChildren(List<FigureFacade> children) {
		this.children = children;
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		if (!model.IsVisible) {
			return;
		}

		renderer.RenderPass(context, pass);
	}
	
	public ChannelOutputs UpdateFrame(DeviceContext context, FrameUpdateParameters updateParameters, ChannelOutputs parentOutputs) {
		var previousFrameResults = controlVertexProvider.GetPreviousFrameResults(context);

		ChannelInputs shapeInputs = new ChannelInputs(model.Shape.ChannelInputs);

		foreach (var child in children) {
			if (child.Model.IsVisible) {
				child.Model.Shape.ApplyOverrides(shapeInputs);
			}
		}

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

		[JsonProperty("visible", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(true)]
		public bool isVisible;

		[JsonProperty("shape", DefaultValueHandling = DefaultValueHandling.Ignore)]
		[DefaultValue(Shape.DefaultLabel)]
		public string shape;
		
		[JsonProperty("material-set")]
		public string materialSet;

		public void Merge(FigureFacade figure) {
			figure.Model.IsVisible = isVisible;

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
			isVisible = model.IsVisible,
			shape = model.ShapeName,
			materialSet = model.MaterialSetName
		};
	}
}
