using System;
using System.Collections.Generic;

public class ChannelImporter {
	private readonly DsonObjectLocator locator;
	private readonly string rootScope;

	private readonly List<ChannelRecipe> channelRecipes = new List<ChannelRecipe>();

	public ChannelImporter(DsonObjectLocator locator, string rootScope) {
		this.locator = locator;
		this.rootScope = rootScope;
	}

	public IEnumerable<ChannelRecipe> ChannelRecipes => channelRecipes;

	public void Import(DsonTypes.Channel channel, string expectedId, string name, string pathPrefix) {
		if (channel.id != expectedId) {
			throw new InvalidOperationException("channel has unexpected id");
		}

		if (channel.target_channel != null) {
			//Skip alias channels
			return;
		}

		string label = channel.label ?? channel.name;
		string path = pathPrefix + "/" + label;
				
		ChannelRecipe recipe = new ChannelRecipe {
			Name = name,
			InitialValue = channel.value,
			Min = channel.min,
			Max = channel.max,
			Clamped = channel.clamped,
			Visible = channel.visible,
			Path = path
		};

		channelRecipes.Add(recipe);
	}

	public void ImportTriplet(DsonTypes.Channel[] triplet, string namePrefix, string pathPrefix) {
		if (triplet.Length != 3) {
			throw new InvalidOperationException("expected 3 channels per triplet");
		}

		Import(triplet[0], "x", namePrefix + "/x", pathPrefix);
		Import(triplet[1], "y", namePrefix + "/y", pathPrefix);
		Import(triplet[2], "z", namePrefix + "/z", pathPrefix);
	}

	public void ImportFrom(DsonTypes.Node node) {
		string label = node.label ?? node.name;
		string pathPrefix = "/Joints/" + label;

		ImportTriplet(node.center_point, node.name + "?center_point", pathPrefix);
		ImportTriplet(node.end_point, node.name + "?end_point", pathPrefix);
		ImportTriplet(node.orientation, node.name + "?orientation", pathPrefix);
		ImportTriplet(node.rotation, node.name + "?rotation", pathPrefix);
		ImportTriplet(node.translation, node.name + "?translation", pathPrefix);
		ImportTriplet(node.scale, node.name + "?scale", pathPrefix);
		Import(node.general_scale, "general_scale", node.name + "?scale/general", pathPrefix);
	}

	public void ImportFrom(DsonTypes.Modifier modifier) {
		if (modifier.channel == null) {
			return;
		}

		string scope;
		DsonTypes.DsonObject parent = modifier.parent.ReferencedObject;
		if (parent is DsonTypes.Node) {
			if (parent.id == rootScope) {
				scope = null;
			} else {
				scope = parent.id;
			}
		} else {
			//if parent is not a node, then assume it's a geometry attached to the root node
			scope = null;
		}
		
		string pathPrefix = "";
		if (modifier.presentation?.type?.StartsWith("Modifier/Shape") ?? false) {
			pathPrefix += "/Shapes";
		}

		if (modifier.region != null) {
			pathPrefix += "/" + modifier.region;
		}

		if (modifier.group != null) {
			pathPrefix += modifier.group;
		}

		string name = modifier.name;
		if (scope != null) {
			name = scope + ":" + name;
		}
		
		Import(modifier.channel, "value", name + "?value", pathPrefix);
	}

	public void ImportFrom(DsonTypes.Node[] nodes) {
		if (nodes == null) {
			return;
		}

		foreach (var node in nodes) {
			ImportFrom(node);
		}
	}

	public void ImportFrom(DsonTypes.Modifier[] modifiers) {
		if (modifiers == null) {
			return;
		}

		foreach (var modifier in modifiers) {
			ImportFrom(modifier);
		}
	}

	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.node_library);
		ImportFrom(doc.Root.modifier_library);
	}

	public static IEnumerable<ChannelRecipe> ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		ChannelImporter importer = new ChannelImporter(locator, figureUris.RootNodeId);

		importer.ImportFrom(locator.LocateRoot(figureUris.DocumentUri));
		foreach (DsonTypes.DsonDocument doc in locator.GetAllDocumentsUnderPath(figureUris.MorphsBasePath)) {
			importer.ImportFrom(doc);
		}

		return importer.ChannelRecipes;
	}
}
