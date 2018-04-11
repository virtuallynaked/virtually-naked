using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class OutfitItemJsonProxy {
	[JsonProperty(PropertyName = "figure")]
	public string figure;

	[JsonProperty(PropertyName = "label")]
	public string label;

	[JsonProperty(PropertyName = "is-initially-visible")]
	public bool isInitiallyVisible = true;
}

public class OutfitJsonProxy {
	[JsonProperty(PropertyName = "label")]
	public string label;

	[JsonProperty(PropertyName = "items")]
	public List<OutfitItemJsonProxy> items = new List<OutfitItemJsonProxy>();
}

public class OutfitImporter {
	public static void ImportAll() {
		var outfitsDir = CommonPaths.WorkDir.Subdirectory("outfits");
		outfitsDir.CreateWithParents();

		foreach (var outfitFile in CommonPaths.OutfitsDir.GetFiles()) {
			string name = outfitFile.GetNameWithoutExtension();
			var destinationFile = outfitsDir.File(name + ".dat");
			if (destinationFile.Exists) {
				continue;
			}

			string json = outfitFile.ReadAllText();
			var proxy = JsonConvert.DeserializeObject<OutfitJsonProxy>(json);

			var items = proxy.items
				.Select(itemProxy => {
					ImportProperties.Load(itemProxy.figure); //verify that that a figure with this name exists
					return new Outfit.Item(itemProxy.figure, itemProxy.label, itemProxy.isInitiallyVisible);
				})
				.ToList();

			var outfit = new Outfit(proxy.label, items);

			Persistance.Save(destinationFile, outfit);
		}
	}
}
