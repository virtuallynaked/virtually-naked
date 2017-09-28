using System;
using System.Collections.Generic;

public class FigureModel {
	public static FigureModel Load(IArchiveDirectory figureDir, string shapeName, string materialSetName, string animationName, FigureModel parent) {
		FigureDefinition definition = FigureDefinition.Load(figureDir, parent?.definition);
		ShapesModel shapesModel = ShapesModel.Load(figureDir, definition.ChannelSystem, shapeName);
		MaterialsModel materialsModel = MaterialsModel.Load(figureDir, materialSetName);
		AnimationModel animationModel = AnimationModel.Load(figureDir, definition.BoneSystem, animationName);
		BehaviorModel behaviorModel = parent == null ? new BehaviorModel() : null;
		return new FigureModel(definition, shapesModel, materialsModel, animationModel, behaviorModel);
	}

	private readonly FigureDefinition definition;
	private readonly ShapesModel shapes;
	private readonly MaterialsModel materials;
	private readonly AnimationModel animation;
	private readonly BehaviorModel behavior;
	
	private readonly ChannelInputs inputs;

	public FigureModel(FigureDefinition definition, ShapesModel shapes, MaterialsModel materials, AnimationModel animation, BehaviorModel behavior) {
		this.definition = definition;

		this.shapes = shapes;
		this.materials = materials;
		this.animation = animation;
		this.behavior = behavior;

		var initialInputs = shapes.Active.ChannelInputs;
		
		inputs = new ChannelInputs(initialInputs);
		
		//hack to turn on eCTRLConfident at start
		if (definition.ChannelSystem.Parent == null) {
			definition.ChannelSystem.ChannelsByName["eCTRLConfident?value"].SetValue(inputs, 1);
		}

		shapes.ShapeChanged += OnShapeChanged;
	}
	
	public FigureDefinition Definition => definition;
	public ChannelInputs Inputs => inputs;
	public ShapesModel Shapes => shapes;
	public MaterialsModel Materials => materials;
	public AnimationModel Animation => animation;
	public BehaviorModel Behavior => behavior;

	private static bool IsShapeChannel(Channel channel) {
		return channel.Path != null && channel.Path.StartsWith("/Shapes/");
	}
	
	private static bool IsExpressionChannel(Channel channel) {
		return channel.Path != null && channel.Path.StartsWith("/Pose Controls/Head/Expressions");
	}

	public event Action ShapeReset;
	public event Action PoseReset;

	public void ResetShape() {
		var initialInputs = shapes.Active.ChannelInputs;
		foreach (Channel channel in definition.ChannelSystem.Channels) {
			if (IsShapeChannel(channel)) {
				inputs.RawValues[channel.Index] = initialInputs.RawValues[channel.Index];
			}
		}
		ShapeReset?.Invoke();
	}

	public void ResetPose() {
		var initialInputs = shapes.Active.ChannelInputs;
		foreach (Channel channel in definition.ChannelSystem.Channels) {
			if (!IsShapeChannel(channel) && !IsExpressionChannel(channel)) {
				inputs.RawValues[channel.Index] = initialInputs.RawValues[channel.Index];
			}
		}
		PoseReset?.Invoke();
	}

	private void OnShapeChanged(Shape oldShape, Shape newShape) {
		var oldInputs = oldShape.ChannelInputs;
		var newInputs = newShape.ChannelInputs;

		foreach (Channel channel in definition.ChannelSystem.Channels) {
			int idx = channel.Index;
			inputs.RawValues[idx] += newInputs.RawValues[idx] - oldInputs.RawValues[idx];
		}
	}

	public Dictionary<string, double> UserValues {
		get {
			Dictionary<string, double> userValues = new Dictionary<string, double>();
			var initialInputs = shapes.Active.ChannelInputs;
			foreach (Channel channel in definition.ChannelSystem.Channels) {
				int idx = channel.Index;
				double userValue = inputs.RawValues[idx] - initialInputs.RawValues[idx];
				if (userValue != 0) {
					userValues.Add(channel.Name, userValue);
				}
			}
			return userValues;
		}
		set {
			var initialInputs = shapes.Active.ChannelInputs;
			Dictionary<string, double> userValues = value;

			foreach (Channel channel in definition.ChannelSystem.Channels) {
				int idx = channel.Index;

				double initialInput = initialInputs.RawValues[channel.Index];
				userValues.TryGetValue(channel.Name, out double userValue);

				inputs.RawValues[idx] = userValue + initialInput;
			}
		}
	}
}
