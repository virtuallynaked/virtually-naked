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

	public void SetValue(ChannelInputs inputs, Vector3 value, SetMask mask = SetMask.Any) {
		X.SetValue(inputs, value.X, mask);
		Y.SetValue(inputs, value.Y, mask);
		Z.SetValue(inputs, value.Z, mask);
	}

	public void SetEffectiveValue(ChannelInputs inputs, ChannelOutputs outputsForDelta, Vector3 value, SetMask mask = SetMask.Any) {
		X.SetEffectiveValue(inputs, outputsForDelta, value.X, mask);
		Y.SetEffectiveValue(inputs, outputsForDelta, value.Y, mask);
		Z.SetEffectiveValue(inputs, outputsForDelta, value.Z, mask);
	}
}

