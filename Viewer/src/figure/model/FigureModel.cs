using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FigureModel {
	public static FigureModel Load(IArchiveDirectory figureDir, string shapeName, string materialSetName, string animationName, FigureModel parent) {
		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		var channelSystem = channelSystemRecipe.Bake(parent?.channelSystem);

		BoneSystem boneSystem;
		if (parent != null) {
			boneSystem = parent.boneSystem;
		} else {
			var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
			boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);
		}
		
		ShapesModel shapesModel = ShapesModel.Load(figureDir, channelSystem, shapeName);
		MaterialsModel materialsModel = MaterialsModel.Load(figureDir, materialSetName);
		AnimationModel animationModel = AnimationModel.Load(figureDir, boneSystem, animationName);
		return new FigureModel(channelSystem, boneSystem, shapesModel, materialsModel, animationModel);
	}

	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;

	private readonly ShapesModel shapes;
	private readonly MaterialsModel materials;
	private readonly AnimationModel animation;
	
	private readonly ChannelInputs inputs;

	public FigureModel(ChannelSystem channelSystem, BoneSystem boneSystem, ShapesModel shapes, MaterialsModel materials, AnimationModel animation) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		this.shapes = shapes;
		this.materials = materials;
		this.animation = animation;

		var initialInputs = shapes.Active.ChannelInputs;
		
		inputs = new ChannelInputs(initialInputs);
		
		//hack to turn on eCTRLConfident at start
		if (channelSystem.Parent == null) {
			channelSystem.ChannelsByName["eCTRLConfident?value"].SetValue(inputs, 1);
		}

		shapes.ShapeChanged += OnShapeChanged;
	}
	
	public ChannelSystem ChannelSystem => channelSystem;
	public BoneSystem BoneSystem => boneSystem;
	public ChannelInputs Inputs => inputs;
	public ShapesModel Shapes => shapes;
	public MaterialsModel Materials => materials;
	public AnimationModel Animation => animation;

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
		foreach (Channel channel in channelSystem.Channels) {
			if (IsShapeChannel(channel)) {
				inputs.RawValues[channel.Index] = initialInputs.RawValues[channel.Index];
			}
		}
		ShapeReset?.Invoke();
	}

	public void ResetPose() {
		var initialInputs = shapes.Active.ChannelInputs;
		foreach (Channel channel in channelSystem.Channels) {
			if (!IsShapeChannel(channel) && !IsExpressionChannel(channel)) {
				inputs.RawValues[channel.Index] = initialInputs.RawValues[channel.Index];
			}
		}
		PoseReset?.Invoke();
	}

	private void OnShapeChanged(Shape oldShape, Shape newShape) {
		var oldInputs = oldShape.ChannelInputs;
		var newInputs = newShape.ChannelInputs;

		foreach (Channel channel in channelSystem.Channels) {
			int idx = channel.Index;
			inputs.RawValues[idx] += newInputs.RawValues[idx] - oldInputs.RawValues[idx];
		}
	}
}
