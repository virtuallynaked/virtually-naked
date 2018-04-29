using System.Collections.Generic;

public class Channel {
	private const bool OverrideLimitsOnJoints = false; //for debugging purposes

	public string Name {get; }
	public int Index {get; }
	public Channel ParentChannel {get;}
	public double InitialValue {get;}
	public double Min {get;}
	public double Max {get;}
	public bool Clamped {get;}
	public bool Visible {get;}
	public bool Locked {get;}
	public string Path {get;}
	
	private List<Formula> sumFormulas = new List<Formula>();
	private List<Formula> multiplyFormulas = new List<Formula>();
	
	public Channel(string name, int index, Channel parentChannel, double initialValue, double min, double max, bool clamped, bool visible, bool locked, string path) {
		bool isJoint = path != null && path.StartsWith("/Joints/");
		bool overrideLimit = isJoint && OverrideLimitsOnJoints;

		Name = name;
		Index = index;
		ParentChannel = parentChannel;
		InitialValue = initialValue;
		Min = min;
		Max = max;
		Clamped = clamped && !overrideLimit;
		Visible = visible;
		Locked = locked || overrideLimit;
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
		if (Locked) {
			return;
		}
		if (mask.HasFlag(SetMask.ApplyClamp) && Clamped) {
			value = EvaluatorHelperMethods.Clamp(value, Min, Max);
		}

		inputs.RawValues[this.Index] = value;
	}

	public void AddValue(ChannelInputs inputs, double delta, SetMask mask = SetMask.Any) {
		double current = inputs.RawValues[Index];
		SetValue(inputs, current + delta, mask);
	}

	public void SetEffectiveValue(ChannelInputs inputs, ChannelOutputs outputsForDelta, double value, SetMask mask = SetMask.Any) {
		if (Locked) {
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
