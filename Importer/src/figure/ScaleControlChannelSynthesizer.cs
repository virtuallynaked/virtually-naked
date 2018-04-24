using System.Collections.Generic;

public class ScaleControlChannelSynthesizer {
	private const string ChannelName = "CTRLBodyScale?value";
	private List<BoneRecipe> bones;

	public ScaleControlChannelSynthesizer(List<BoneRecipe> bones) {
		this.bones = bones;
	}

	public ChannelRecipe SynthesizeChannel() {
		return new ChannelRecipe {
			Name = ChannelName,
			Path = "/Shapes/Full Body/Scale",
			Visible = true,
			Min = -1,
			Max = +1,
			Clamped = true,
			InitialValue = 0
		};
	}

	public FormulaRecipe SynthesizeFormula() {
		var rootBoneScaleChannelName = bones[0].Name + "?scale/general";

		return new FormulaRecipe {
			Output = rootBoneScaleChannelName,
			Stage = FormulaRecipe.FormulaStage.Multiply,
			Operations = new List<OperationRecipe> {
				OperationRecipe.MakePushChannel(ChannelName),
				OperationRecipe.MakePushConstant(1),
				OperationRecipe.Make(OperationRecipe.OperationKind.Add)
			}
		};
	}
}
