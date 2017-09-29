using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class Actor : IDisposable {
	public static Actor Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager) {
		var mainFigure = FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Main, null);

		var actorModel = ActorModel.Load(mainFigure.Definition, FigureActiveSettings.Animation);

		var hairFigure = FigureActiveSettings.Hair != null ? FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Hair, mainFigure) : null;

		var clothingFigures = FigureActiveSettings.Clothing
			.Select(figureName => FigureFacade.Load(dataDir, device, shaderCache, controllerManager, figureName, mainFigure))
			.ToArray();
		
		var behavior = ActorBehavior.Load(controllerManager, mainFigure.Definition.Directory, actorModel);

		return new Actor(device, actorModel, mainFigure, hairFigure, clothingFigures, behavior);
	}

	class MainFigureAnimator : IFigureAnimator {
		private readonly Actor actor;

		public MainFigureAnimator(Actor actor) {
			this.actor = actor;
		}

		public ChannelInputs GetFrameInputs(ChannelInputs shapeInputs, FrameUpdateParameters updateParameters, ControlVertexInfo[] previousFrameControlVertexInfos) {
			return actor.Behavior.Update(shapeInputs, updateParameters, previousFrameControlVertexInfos);
		}
	}
	
	private readonly ActorModel model;

	private readonly FigureFacade mainFigure;
	private readonly FigureFacade hairFigure;
	private readonly FigureFacade[] clothingFigures;

	private readonly FigureGroup figureGroup;

	private readonly ActorBehavior behavior;
	
	public Actor(Device device, ActorModel model, FigureFacade mainFigure, FigureFacade hairFigure, FigureFacade[] clothingFigures, ActorBehavior behavior) {
		this.model = model;
		this.mainFigure = mainFigure;
		this.hairFigure = hairFigure;
		this.clothingFigures = clothingFigures;

		var childFigures = Enumerable.Repeat(hairFigure, hairFigure == null ? 0 : 1)
			.Concat(clothingFigures)
			.ToArray();
		figureGroup = new FigureGroup(device, mainFigure, childFigures);

		this.behavior = behavior;

		mainFigure.Animator = new MainFigureAnimator(this);
	}

	public void Dispose() {
		figureGroup.Dispose();
	}
	
	public ActorModel Model => model;
	public FigureFacade Main => mainFigure;
	public FigureFacade Hair => hairFigure;
	public ActorBehavior Behavior => behavior;
	
	public IMenuLevel MenuLevel => ActorMenuProvider.MakeRootMenuLevel(this);
	
	public void RenderPass(DeviceContext context, RenderingPass pass) {
		figureGroup.RenderPass(context, pass);
	}

	public void Update(DeviceContext context, FrameUpdateParameters updateParameters, ImageBasedLightingEnvironment iblEnvironment) {
		figureGroup.Update(context, updateParameters, iblEnvironment);
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

		[JsonProperty("pose")]
		public ActorBehavior.PoseRecipe pose;

		public void Merge(Actor actor) {
			main?.Merge(actor.Main);
			hair?.Merge(actor.Hair);
			if (animation != null) {
				actor.model.Animation.ActiveName = animation;
			}
			behaviour?.Merge(actor.model.Behavior);
			if (channelValues != null) {
				actor.model.UserValues = channelValues;
			}
			pose?.Merge(actor.Behavior);
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			main = Main.Recipize(),
			hair = Hair?.Recipize(),
			animation = model.Animation.ActiveName,
			behaviour = model.Behavior.Recipize(),
			channelValues = model.UserValues,
			pose = Behavior.RecipizePose()
		};
	}
}