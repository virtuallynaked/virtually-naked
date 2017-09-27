using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

class Actor : IDisposable {
	public static Actor Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager) {
		var mainFigure = FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Main, null);

		var hairFigure = FigureActiveSettings.Hair != null ? FigureFacade.Load(dataDir, device, shaderCache, controllerManager, FigureActiveSettings.Hair, mainFigure) : null;

		var clothingFigures = FigureActiveSettings.Clothing
			.Select(figureName => FigureFacade.Load(dataDir, device, shaderCache, controllerManager, figureName, mainFigure))
			.ToArray();
		
		return new Actor(device, mainFigure, hairFigure, clothingFigures);
	}
	
	private readonly FigureFacade mainFigure;
	private readonly FigureFacade hairFigure;
	private readonly FigureFacade[] clothingFigures;

	private readonly FigureGroup figureGroup;
	
	public Actor(Device device, FigureFacade mainFigure, FigureFacade hairFigure, FigureFacade[] clothingFigures) {
		this.mainFigure = mainFigure;
		this.hairFigure = hairFigure;
		this.clothingFigures = clothingFigures;

		var childFigures = Enumerable.Repeat(hairFigure, hairFigure == null ? 0 : 1)
			.Concat(clothingFigures)
			.ToArray();
		figureGroup = new FigureGroup(device, mainFigure, childFigures);
	}

	public void Dispose() {
		figureGroup.Dispose();
	}
	
	public FigureFacade Main => mainFigure;
	public FigureFacade Hair => hairFigure;
	
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
		public FigureBehavior.PoseRecipe pose;

		public void Merge(Actor actor) {
			main?.Merge(actor.Main);
			hair?.Merge(actor.Hair);
			if (animation != null) {
				actor.Main.Model.Animation.ActiveName = animation;
			}
			behaviour?.Merge(actor.Main.Model.Behavior);
			if (channelValues != null) {
				actor.Main.Model.UserValues = channelValues;
			}
			pose?.Merge(actor.Main.Behaviour);
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			main = Main.Recipize(),
			hair = Hair?.Recipize(),
			animation = Main.Model.Animation.ActiveName,
			behaviour = Main.Model.Behavior.Recipize(),
			channelValues = Main.Model.UserValues,
			pose = Main.Behaviour.RecipizePose()
		};
	}
}