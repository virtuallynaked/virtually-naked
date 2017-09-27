using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

class FigureGroup : IDisposable {
	public static FigureGroup Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager) {
		var parentFigure = FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Parent, null);

		var hairFigure = FigureActiveSettings.Hair != null ? FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Hair, parentFigure) : null;

		var clothingFigures = FigureActiveSettings.Clothing
			.Select(figureName => FigureFacade.Load(dataDir, device, shaderCache, controllerManager, figureName, parentFigure))
			.ToArray();
		
		return new FigureGroup(device, controllerManager, parentFigure, hairFigure, clothingFigures);
	}
	
	private readonly FigureFacade parentFigure;
	private readonly FigureFacade hairFigure;
	private readonly FigureFacade[] clothingFigures;
	
	private readonly FigureFacade[] childFigures;

	private readonly CoordinateNormalMatrixPairConstantBufferManager modelToWorldTransform;
	private readonly InOutStructuredBufferManager<Vector3> parentDeltas;

	public FigureGroup(Device device, ControllerManager controllerManager, FigureFacade parentFigure, FigureFacade hairFigure, FigureFacade[] clothingFigures) {
		this.parentFigure = parentFigure;
		this.hairFigure = hairFigure;
		this.clothingFigures = clothingFigures;

		childFigures = Enumerable.Repeat(hairFigure, hairFigure == null ? 0 : 1)
			.Concat(clothingFigures)
			.ToArray();

		this.modelToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);
		this.parentDeltas = new InOutStructuredBufferManager<Vector3>(device, parentFigure.VertexCount);
		
		parentFigure.RegisterChildren(childFigures.ToList());
	}

	public FigureFacade Parent => parentFigure;
	public FigureFacade Hair => hairFigure;

	public IMenuLevel MenuLevel => FigureGroupMenuProvider.MakeRootMenuLevel(this);
	
	public void Dispose() {
		modelToWorldTransform.Dispose();

		parentFigure.Dispose();
		foreach (var figure in childFigures) {
			figure.Dispose();
		}

		parentDeltas.Dispose();
	}

	public void Update(DeviceContext context, FrameUpdateParameters updateParameters, ImageBasedLightingEnvironment lightingEnvironment) {
		modelToWorldTransform.Update(context, Matrix.Scaling(0.01f));
		
		var parentOutputs = parentFigure.UpdateFrame(updateParameters, null);
		foreach (var figure in childFigures) {
			figure.UpdateFrame(updateParameters, parentOutputs);
		}
		
		parentFigure.UpdateVertexPositionsAndGetDeltas(parentDeltas.OutView);
		foreach (var figure in childFigures) {
			figure.UpdateVertexPositions(parentDeltas.InView);
		}
		
		parentFigure.Update(context, lightingEnvironment);
		foreach (var figure in childFigures) {
			figure.Update(context, lightingEnvironment);
		}
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		context.VertexShader.SetConstantBuffer(1, modelToWorldTransform.Buffer);

		parentFigure.RenderPass(context, pass);
		foreach (var figure in childFigures) {
			figure.RenderPass(context, pass);
		}
	}

	public class Recipe {
		[JsonProperty("main")]
		public FigureFacade.Recipe main;

		[JsonProperty("hair")]
		public FigureFacade.Recipe hair;

		[JsonProperty("animation")]
		public string animation;

		[JsonProperty("behaviour")]
		public BehaviorModel.Recipe behaviour;

		[JsonProperty("channel-values")]
		public Dictionary<string, double> channelValues;

		public void Merge(FigureGroup group) {
			main?.Merge(group.Parent);
			hair?.Merge(group.Hair);
			if (animation != null) {
				group.Parent.Model.Animation.ActiveName = animation;
			}
			behaviour?.Merge(group.Parent.Model.Behavior);
			if (channelValues != null) {
				group.Parent.Model.UserValues = channelValues;
			}
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			main = Parent.Recipize(),
			hair = Hair?.Recipize(),
			animation = Parent.Model.Animation.ActiveName,
			behaviour = Parent.Model.Behavior.Recipize(),
			channelValues = Parent.Model.UserValues
		};
	}
}
