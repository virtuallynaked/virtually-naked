using System.Collections.Generic;

public class Channel {
	public string Name {get; }
	public int Index {get; }
	public Channel ParentChannel {get;}
	public double InitialValue {get;}
	public double Min {get;}
	public double Max {get;}
	public bool Clamped {get;}
	public bool Visible {get;}
	public string Path {get;}
	
	private List<Formula> sumFormulas = new List<Formula>();
	private List<Formula> multiplyFormulas = new List<Formula>();
	
	public Channel(string name, int index, Channel parentChannel, double initialValue, double min, double max, bool clamped, bool visible, string path) {
		Name = name;
		Index = index;
		ParentChannel = parentChannel;
		InitialValue = initialValue;
		Min = min;
		Max = max;
		Clamped = clamped;
		Visible = visible;
		Path = path;
	}
	
	public List<Formula> SumFormulas => sumFormulas;
	public List<Formula> MultiplyFormulas => multiplyFormulas;

	public void AttachSumFormula(Formula formula) {
		sumFormulas.Add(formula);
	}

	public void AttachMultiplyFormula(Formula formula) {
		multiplyFormulas.Add(formula);
	}
	
	public void SetValue(ChannelInputs inputs, double value, SetMask mask = SetMask.Any) {
		if (mask.HasFlag(SetMask.VisibleOnly) && !Visible) {
			return;
		}
		if (mask.HasFlag(SetMask.ApplyClamp) && Clamped) {
			value = EvaluatorHelperMethods.Clamp(value, Min, Max);
		}

		inputs.RawValues[this.Index] = value;
	}

	public void SetEffectiveValue(ChannelInputs inputs, ChannelOutputs outputsForDelta, double value, SetMask mask = SetMask.Any) {
		if (mask.HasFlag(SetMask.VisibleOnly) && !Visible) {
			return;
		}

		if (mask.HasFlag(SetMask.ApplyClamp) && Clamped) {
			value = EvaluatorHelperMethods.Clamp(value, Min, Max);
		}

		double delta = value - outputsForDelta.Values[this.Index];

		inputs.RawValues[this.Index] += delta;
	}

	public double GetValue(ChannelOutputs outputs) {
		return outputs.Values[this.Index];
	}

	public double GetInputValue(ChannelInputs inputs) {
		return inputs.RawValues[this.Index];
	}
}
