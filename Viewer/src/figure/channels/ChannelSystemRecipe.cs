using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ChannelSystemRecipe {
	public List<ChannelRecipe> Channels { get; }
	public List<FormulaRecipe> Formulas { get; }

	public ChannelSystemRecipe(List<ChannelRecipe> channels, List<FormulaRecipe> formulas) {
		Channels = channels ?? new List<ChannelRecipe>();
		Formulas = formulas ?? new List<FormulaRecipe>();
	}

	public ChannelSystem Bake(ChannelSystem parent) {
		var channels = new List<Channel>();
		Channels.ForEach(recipe => recipe.Bake(channels, parent?.ChannelsByName));

		var channelsByName = channels.ToDictionary(channel => channel.Name, channel => channel);
		Formulas.ForEach(recipe => recipe.Bake(channelsByName));

		return new ChannelSystem(parent, channels);
	}
}
