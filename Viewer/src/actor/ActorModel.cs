using System;
using System.Collections.Generic;

public class ActorModel {
	public static ActorModel Load(FigureDefinition definition, string initialAnimationName) {
		AnimationModel animationModel = AnimationModel.Load(definition, initialAnimationName);
		BehaviorModel behaviorModel = new BehaviorModel();
		return new ActorModel(definition, animationModel, behaviorModel);
	}

	private readonly FigureDefinition mainDefinition;

	private readonly AnimationModel animation;
	private readonly BehaviorModel behavior;
	
	private readonly ChannelInputs inputs;

	public ActorModel(FigureDefinition mainDefinition, AnimationModel animation, BehaviorModel behavior) {
		this.mainDefinition = mainDefinition;

		this.animation = animation;
		this.behavior = behavior;
				
		inputs = mainDefinition.ChannelSystem.MakeZeroChannelInputs();
		
		//hack to turn on eCTRLConfident at start
		mainDefinition.ChannelSystem.ChannelsByName["eCTRLConfident?value"].SetValue(inputs, 1);
	}
	
	public FigureDefinition MainDefinition => mainDefinition;
	public ChannelInputs Inputs => inputs;
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
		foreach (Channel channel in mainDefinition.ChannelSystem.Channels) {
			if (IsShapeChannel(channel)) {
				inputs.RawValues[channel.Index] = 0;
			}
		}
		ShapeReset?.Invoke();
	}

	public void ResetPose() {
		foreach (Channel channel in mainDefinition.ChannelSystem.Channels) {
			if (!IsShapeChannel(channel) && !IsExpressionChannel(channel)) {
				inputs.RawValues[channel.Index] = 0;
			}
		}
		PoseReset?.Invoke();
	}
	
	public Dictionary<string, double> UserValues {
		get {
			Dictionary<string, double> userValues = new Dictionary<string, double>();
			foreach (Channel channel in mainDefinition.ChannelSystem.Channels) {
				int idx = channel.Index;
				double userValue = inputs.RawValues[idx];
				if (userValue != 0) {
					userValues.Add(channel.Name, userValue);
				}
			}
			return userValues;
		}
		set {
			Dictionary<string, double> userValues = value;

			foreach (Channel channel in mainDefinition.ChannelSystem.Channels) {
				int idx = channel.Index;
				userValues.TryGetValue(channel.Name, out double userValue);
				inputs.RawValues[idx] = userValue;
			}
		}
	}
}
