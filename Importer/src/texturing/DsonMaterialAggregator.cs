using System;
using System.Collections.Generic;

class DsonMaterialAggregator {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly Dictionary<string, DsonTypes.Image> imagesByUrl;
	private readonly Dictionary<string, MaterialBag> bags;

	public DsonMaterialAggregator(ContentFileLocator fileLocator, DsonObjectLocator objectLocator) : this(
		fileLocator, objectLocator, 
		new Dictionary<string, DsonTypes.Image>(),
		new Dictionary<string, MaterialBag>()) {
	}

	public DsonMaterialAggregator(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Dictionary<string, DsonTypes.Image> imagesByUrl, Dictionary<string, MaterialBag> bags) {
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.imagesByUrl = imagesByUrl;
		this.bags = bags;
	}

	public DsonMaterialAggregator Branch() {
		Dictionary<string, MaterialBag> branchedBags = new Dictionary<string, MaterialBag>();
		foreach (var entry in bags) {
			var branchedBag = entry.Value.Branch();
			branchedBags.Add(entry.Key, branchedBag);
		}

		return new DsonMaterialAggregator(fileLocator, objectLocator, imagesByUrl, branchedBags);
	}
	
	private MaterialBag MakeBag(string materialName) {
		if (!bags.TryGetValue(materialName, out var bag)) {
			bag = new MaterialBag(fileLocator, objectLocator, imagesByUrl);
			bags.Add(materialName, bag);
		}

		return bag;
	}

	private void IncludeMaterialChannel(MaterialBag bag, string path, DsonTypes.MaterialChannelChannel channel) {
		if (channel == null) {
			return;
		}

		string channelName = path + channel.id;

		bag.SetValue(channelName, "image_file", channel.image_file);
		bag.SetValue(channelName, "value", channel.current_value);

		float scale;
		if (channel.image_modification != null) {
			scale = channel.image_modification.scale;
		} else {
			scale = 1;
		}
		bag.SetValue(channelName, "image_modification/scale", scale);
	}

	public void IncludeDuf(DsonTypes.DsonRoot root) {
		foreach (DsonTypes.Image image in Utils.SafeEnumerable(root.image_library)) {
			string imagePath = image.map[0].url;
			if (image.map.Length != 1) {
				throw new InvalidOperationException("expected only one image per map");
			}
			if (!imagesByUrl.ContainsKey(imagePath)) {
				imagesByUrl[imagePath] = image;
			}
		}

		foreach (var instance in (root.scene.materials ?? new DsonTypes.MaterialInstance[0])) {
			DsonTypes.Material material = instance.url.LocateReferencedObject(false);
			if (material == null) {
				continue;
			}

			if (instance.groups.Length != 1) {
				throw new Exception("expected exactly 1 group");
			} 

			string surfaceName = instance.groups[0];
			var bag = MakeBag(surfaceName);

			bag.SetValue("uv_set", material.uv_set);
			bag.SetValue("uv_set", instance.uv_set);

			IncludeMaterialChannel(bag, "", material.diffuse?.channel);
			IncludeMaterialChannel(bag, "", instance.diffuse?.channel);

			foreach (var extra in Utils.SafeEnumerable(material.extra)) {
				string type = extra.type;
				bag.AddExtraType(type);
				foreach (var channel in Utils.SafeArray(extra.channels)) {
					IncludeMaterialChannel(bag, $"extra/{type}/channels/", channel.channel);
				}
			}
			foreach (var extra in Utils.SafeEnumerable(instance.extra)) {
				string type = extra.type;
				bag.AddExtraType(type);
				foreach (var channel in Utils.SafeArray(extra.channels)) {
					IncludeMaterialChannel(bag, $"extra/{type}/channels/", channel.channel);
				}
			}
		}

		foreach (DsonTypes.ChannelAnimation animation in (root.scene.animations ?? new DsonTypes.ChannelAnimation[0])) {
			string url = animation.url;

			url = url.Substring(url.IndexOf('#') + 1);

			string expectedPrefix = "materials/";
			if (!url.StartsWith(expectedPrefix)) {
				continue;
			}
			url = url.Substring(expectedPrefix.Length);

			string materialNameSeparator = ":?";
			int materialNameSeparatorIdx = url.IndexOf(materialNameSeparator);
			if (materialNameSeparatorIdx == -1) {
				continue;
			}

			string materialName = Uri.UnescapeDataString(url.Substring(0, materialNameSeparatorIdx));
			url = url.Substring(materialNameSeparatorIdx + materialNameSeparator.Length);
			
			var bag = MakeBag(materialName);

			object value = animation.keys[0][1];


			if (url.EndsWith("/image") && value == null) {
				// Setting ".../image" to null causes all image-related settings to be cleared
				// This comes up with Sorbet Swimsuit Style 4, which has no image (just a solid color) for the bra
				bag.RemoveByUrl(url + "_file");
			} else {
				bag.SetByUrl(url, value);
			}
		}
	}

	public MaterialBag GetBag(string materialName) {
		return bags[materialName];
	}
}
