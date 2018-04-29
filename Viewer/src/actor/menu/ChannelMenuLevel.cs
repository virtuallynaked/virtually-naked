using System;
using System.Collections.Generic;

public class ChannelMenuLevel : IMenuLevel {
	private static readonly HashSet<string> SkipCategories = new HashSet<string> { "Real World", "Fantasy SciFi", "Actor" };

	private ActorModel model;
	public Dictionary<string, ChannelMenuLevel> SubLevels { get; } = new Dictionary<string, ChannelMenuLevel>();
	public Dictionary<string, Channel> Channels { get; } = new Dictionary<string, Channel>();

	public static ChannelMenuLevel MakeRootLevelForFigure(ActorModel model) {
		var rootLevel = new ChannelMenuLevel(model);
		
		foreach (Channel channel in model.MainDefinition.ChannelSystem.Channels) {
			if (!channel.Visible) {
				continue;
			}
			if (channel.Path.StartsWith("/Joints/")) {
				continue;
			}

			var pathArray = channel.Path.Split('/');
			rootLevel.Add(channel, pathArray, 0);
		}

		return rootLevel;
	}

	public ChannelMenuLevel(ActorModel model) {
		this.model = model;
	}
	
	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		List<IMenuItem> items = new List<IMenuItem>();

		foreach (var pair in SubLevels) {
			items.Add(new SubLevelMenuItem(pair.Key, pair.Value));
		}
		foreach (var pair in Channels) {
			items.Add(new ChannelMenuItem(pair.Key, model, pair.Value));
		}

		return items;
	}


	private ChannelMenuLevel Extract(string[] path, int levelIdx) {
		string pathElem = path[levelIdx];

		if (!SubLevels.TryGetValue(pathElem, out ChannelMenuLevel subLevel)) {
			return null;
		}

		if (levelIdx == path.Length - 1) {
			SubLevels.Remove(pathElem);
			return subLevel;
		} else {
			return subLevel.Extract(path, levelIdx + 1);
		}
	}

	public ChannelMenuLevel Extract(string[] path) {
		return Extract(path, 0);
	}

	private void Add(Channel channel, string[] path, int levelIdx) {
		while (path[levelIdx].Length == 0 || SkipCategories.Contains(path[levelIdx])) {
			levelIdx += 1;
		}

		string pathElem = path[levelIdx];
		if (levelIdx == path.Length - 1) {
			Channels[pathElem] = channel;
		} else {
			if (!SubLevels.TryGetValue(pathElem, out var subLevel)) {
				subLevel = new ChannelMenuLevel(model);
				SubLevels.Add(pathElem, subLevel);
			}
			subLevel.Add(channel, path, levelIdx + 1);
		}
	}
}
