using System;
using System.Collections.Generic;
using ProtoBuf;
using System.Linq;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class OperationRecipe {
	public enum OperationKind {
		Push, Add, Sub, Mul, Div, SplineTcb
	}

	public OperationKind Kind { get; set; }
	public double Value { get; set; }
	public string Channel { get; set; }
	public Spline Spline { get; set; }

	public static OperationRecipe MakePushConstant(double value) {
		return new OperationRecipe {
			Kind = OperationKind.Push,
			Value = value
		};
	}

	public static OperationRecipe MakePushChannel(string channel) {
		return new OperationRecipe {
			Kind = OperationKind.Push,
			Channel = channel
		};
	}

	public static OperationRecipe Make(OperationKind kind) {
		return new OperationRecipe {
			Kind = kind
		};
	}

	public static OperationRecipe MakeEvalSpline(Spline spline) {
		return new OperationRecipe {
			Kind = OperationKind.SplineTcb,
			Spline = spline
		};
	}

	public IOperation Bake(Dictionary<string, Channel> channels) {
		switch (Kind) {
			case OperationKind.Push:
				if (Channel != null) {
					return new PushChannelOperation(channels[Channel]);
				} else {
					return new PushValueOperation(Value);
				}
				
			case OperationKind.Add:
				return new AddOperation();

			case OperationKind.Sub:
				return new SubOperation();

			case OperationKind.Mul:
				return new MulOperation();

			case OperationKind.Div:
				return new DivOperation();

			case OperationKind.SplineTcb:
				return new SplineOperation(Spline);

			default:
				throw new InvalidOperationException();
		}
	}
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FormulaRecipe {
	public enum FormulaStage {
		Sum, Multiply
	}

	public string Output { get; set; }
	public FormulaStage Stage { get; set; }
	public List<OperationRecipe> Operations { get; set; }

	public void Bake(Dictionary<string, Channel> channels) {
		var operations = Operations
			.Select(recipe => recipe.Bake(channels))
			.ToArray();
		Formula formula = new Formula(operations);

		Channel outputChannel = channels[Output];
		if (Stage == FormulaStage.Sum) {
			outputChannel.AttachSumFormula(formula);
		} else {
			outputChannel.AttachMultiplyFormula(formula);
		}
	}
}
