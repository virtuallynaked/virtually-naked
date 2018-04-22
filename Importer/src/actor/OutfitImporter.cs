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

	[JsonProperty(PropertyName = "fabrics")]
	public Dictionary<string, Dictionary<string, string>> fabrics;
}

public class OutfitImporter {
	public static void ImportAll(ImporterPathManager pathManager) {
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
					ImportProperties.Load(pathManager, itemProxy.figure); //verify that that a figure with this name exists
					return new Outfit.Item(itemProxy.figure, itemProxy.label, itemProxy.isInitiallyVisible);
				})
				.ToList();

			List<Outfit.Fabric> fabrics = proxy.fabrics?.Select(entry => {
					var label = entry.Key;
					var materialSetsByFigureName = entry.Value;

					//verify that the material set for each figure exists
					foreach (var entry2 in materialSetsByFigureName) {
						var figureName = entry2.Key;
						var materialSetName = entry2.Value;
						var materialSetsConfs = MaterialSetImportConfiguration.Load(pathManager, figureName);
						materialSetsConfs.Where(conf => conf.name == materialSetName).Single(); 
					}

					return new Outfit.Fabric(label, materialSetsByFigureName);
				})
				.ToList();

			var outfit = new Outfit(proxy.label, items, fabrics);

			Persistance.Save(destinationFile, outfit);
		}
	}
}
