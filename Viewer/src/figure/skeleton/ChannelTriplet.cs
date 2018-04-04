using SharpDX;
using System.Collections.Generic;

public class ChannelTriplet {
	public Channel X {get;}
	public Channel Y {get;}
	public Channel Z {get;}

	public static ChannelTriplet Lookup(Dictionary<string, Channel> channels, string namePrefix) {
		Channel x = channels[namePrefix + "/x"];
		Channel y = channels[namePrefix + "/y"];
		Channel z = channels[namePrefix + "/z"];
		return new ChannelTriplet(x, y, z);
	}

	public ChannelTriplet(Channel x, Channel y, Channel z) {
		X = x;
		Y = y;
		Z = z;
	}

	public Vector3 GetValue(ChannelOutputs outputs) {
		return new Vector3(
			(float) X.GetValue(outputs),
			(float) Y.GetValue(outputs),
			(float) Z.GetValue(outputs)
		);
	}

	public Vector3 GetInputValue(ChannelInputs inputs) {
		return new Vector3(
			(float) X.GetInputValue(inputs),
			(float) Y.GetInputValue(inputs),
			(float) Z.GetInputValue(inputs)
		);
	}

	public void SetValue(ChannelInputs inputs, Vector3 value, SetMask mask = SetMask.Any) {
		X.SetValue(inputs, value.X, mask);
		Y.SetValue(inputs, value.Y, mask);
		Z.SetValue(inputs, value.Z, mask);
	}

	public void AddValue(ChannelInputs inputs, Vector3 delta, SetMask mask = SetMask.Any) {
		X.AddValue(inputs, delta.X, mask);
		Y.AddValue(inputs, delta.Y, mask);
		Z.AddValue(inputs, delta.Z, mask);
	}

	public void SetEffectiveValue(ChannelInputs inputs, ChannelOutputs outputsForDelta, Vector3 value, SetMask mask = SetMask.Any) {
		X.SetEffectiveValue(inputs, outputsForDelta, value.X, mask);
		Y.SetEffectiveValue(inputs, outputsForDelta, value.Y, mask);
		Z.SetEffectiveValue(inputs, outputsForDelta, value.Z, mask);
	}
	
	private static void ExtractMinMax(Channel channel, int idx, ref Vector3 min, ref Vector3 max) {
		if (!channel.Visible) {
			min[idx] = (float) channel.InitialValue;
			max[idx] = (float) channel.InitialValue;
		} else if (!channel.Clamped) {
			min[idx] = float.NegativeInfinity;
			max[idx] = float.PositiveInfinity;
		} else {
			min[idx] = (float) channel.Min;
			max[idx] = (float) channel.Max;
		}
	}

	public void ExtractMinMax(out Vector3 min, out Vector3 max) {
		min = Vector3.Zero;
		max = Vector3.Zero;
		ExtractMinMax(X, 0, ref min, ref max);
		ExtractMinMax(Y, 1, ref min, ref max);
		ExtractMinMax(Z, 2, ref min, ref max);
	}
}
