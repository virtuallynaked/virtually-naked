using System;
using System.Linq;

public class ChannelShaker {
	private bool[] isDirectlyUsed;

	public ChannelShaker(ChannelSystem channelSystem) {
		isDirectlyUsed = new bool[channelSystem.Channels.Count];
	}

	public void TagDirectUse(Channel channel) {
		isDirectlyUsed[channel.Index] = true;
	}

	public bool IsChannelUsed(Channel channel) {
		if (isDirectlyUsed[channel.Index]) {
			return true;
		}
		if (channel.ParentChannel != null) {
			return true;
		}

		var dependencyGatheringVisitor = new DependencyGatheringVisitor();
		foreach (var formula in channel.SumFormulas) {
			formula.Accept(dependencyGatheringVisitor);
		}
		foreach (var formula in channel.MultiplyFormulas) {
			formula.Accept(dependencyGatheringVisitor);
		}
		foreach (var dependency in dependencyGatheringVisitor.Dependencies) {
			if (IsChannelUsed(dependency)) {
				return true;
			}
		}
		
		return false;
	}
	
	public static ChannelShaker InitializeFromShapes(Figure figure) {
		var shaker = new ChannelShaker(figure.ChannelSystem);

		ShapeImportConfiguration[] shapeConfigurations = ShapeImportConfiguration.Load(figure.Name);
		foreach (var conf in shapeConfigurations) {
			foreach (var entry in conf.morphs) {
				string channelName = entry.Key + "?value";
				var channel = figure.ChannelsByName[channelName];
				shaker.TagDirectUse(channel);
			}
		}

		return shaker;
	}

	public static bool[] MakeChannelsToIncludeFromShapes(Figure figure) {
		var shaker = InitializeFromShapes(figure);
		return figure.Channels
			.Select(channel => shaker.IsChannelUsed(channel))
			.ToArray();
	}
}
