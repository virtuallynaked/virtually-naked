using ProtoBuf;
using System;
using System.Collections.Generic;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ChannelRecipe {
	public string Name { get; set; }
	public double InitialValue { get; set; }
	public double Min { get; set; }
	public double Max { get; set; }
	public bool Clamped { get; set; }
	public bool Visible { get; set; }
	public string Path { get; set; }

	public void Bake(List<Channel> channels, Dictionary<string, Channel> parentChannels) {
		int index = channels.Count;

		Channel parentChannel;
		if (parentChannels != null) {
			parentChannels.TryGetValue(Name, out parentChannel);
		} else {
			parentChannel = null;
		}

		Channel channel = new Channel(Name, index, parentChannel, InitialValue, Min, Max, Clamped, Visible, Path);
		channels.Add(channel);
	}
	
	/*
	 * Verify a child channel will always have the same value as the parent
	 */
	public static void VerifySimpleChild(ChannelRecipe parentChannel, ChannelRecipe childChannel) {
		if (childChannel.Name != parentChannel.Name) {
			throw new InvalidOperationException("parent and child channels differ");
		}
		if (childChannel.Min > parentChannel.Min) {
			throw new InvalidOperationException("parent and child channels differ");
		}
		if (childChannel.Max < parentChannel.Max) {
			throw new InvalidOperationException("parent and child channels differ");
		}
	}
}
