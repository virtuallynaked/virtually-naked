using System;
using System.Collections.Generic;

public class ChannelInputs {
	public double[] RawValues { get; }
	
	public ChannelInputs(double[] rawValues) {
		RawValues = rawValues;
	}

	public ChannelInputs(ChannelInputs inputs) {
		RawValues = (double[]) inputs.RawValues.Clone();
	}

	public void BlendIn(ChannelInputs inputs, float weight) {
		if (inputs.RawValues.Length != this.RawValues.Length) {
			throw new ArgumentException("length mismatch");
		}
		for (int i = 0; i < RawValues.Length; ++i) {
			RawValues[i] += weight * inputs.RawValues[i];
		}
	}

	public void ClearToZero() {
		for (int i = 0; i < RawValues.Length; ++i) {
			RawValues[i] = 0;
		}
	}
}
