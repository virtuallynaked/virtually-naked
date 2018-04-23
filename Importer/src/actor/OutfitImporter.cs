using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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
	public static void Import(ImporterPathManager pathManager, FileInfo outfitConfFile, DirectoryInfo contentDestDirectory) {
		string name = outfitConfFile.GetNameWithoutExtension();

		var outfitsDir = contentDestDirectory.Subdirectory("outfits");
		var destinationFile = outfitsDir.File(name + ".dat");
		if (destinationFile.Exists) {
			return;
		}

		string json = outfitConfFile.ReadAllText();
		var proxy = JsonConvert.DeserializeObject<OutfitJsonProxy>(json);

		var items = proxy.items
			.Select(itemProxy => {
				ImportProperties.Load(pathManager, itemProxy.figure); //verify that that a figure with this name exists
				return new Outfit.Item(itemProxy.figure, itemProxy.label, itemProxy.isInitiallyVisible);
			})
			.ToList();

		List<Outfit.Fabric> fabrics = proxy.fabrics?
			.Select(entry => {
				var label = entry.Key;
				var materialSetsByFigureName = entry.Value;

				//verify that the material set for each figure exists
				foreach (var entry2 in materialSetsByFigureName) {
					var figureName = entry2.Key;
					var materialSetName = entry2.Value;
					var figureConfDir = pathManager.GetConfDirForFigure(figureName);
					var materialSetsConfs = MaterialSetImportConfiguration.Load(figureConfDir);
					materialSetsConfs.Where(conf => conf.name == materialSetName).Single(); 
				}

				return new Outfit.Fabric(label, materialSetsByFigureName);
			})
			.ToList();

		var outfit = new Outfit(proxy.label, items, fabrics);

		outfitsDir.CreateWithParents();
		Persistance.Save(destinationFile, outfit);
	}
}
