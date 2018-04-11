using System.Collections.Generic;
using System.Linq;

public class Outfit {
	public class Item {
		public string Figure { get; }
		public string Label { get; }
		public bool IsInitiallyVisible { get; }

		public Item(string figure, string label, bool isInitiallyVisible) {
			Figure = figure;
			Label = label;
			IsInitiallyVisible = isInitiallyVisible;
		}
	}

	public class Fabric {
		public string Label { get; }
		public Dictionary<string, string> MaterialSetsByFigure { get; }

		public Fabric(string label, Dictionary<string, string> materialSetsByFigure) {
			Label = label;
			MaterialSetsByFigure = materialSetsByFigure;
		}
	}

	public static Outfit Naked = new Outfit("Naked", new List<Item> { }, null);

	public static List<Outfit> LoadList(IArchiveDirectory dataDir) {
		var outfitsDir = dataDir.Subdirectory("outfits");
		var outfits = outfitsDir.GetFiles()
			.Select(outfitFile => Persistance.Load<Outfit>(outfitFile))
			.Concat(new List<Outfit> { Naked })
			.ToList();
		return outfits;
	}
	
	public bool IsMatch(FigureFacade[] figures) {
		var isMatch = figures.Select(figure => figure.Definition.Name)
			.SequenceEqual(Items.Select(item => item.Figure));
		return isMatch;
	}

	public string Label { get; }
	public List<Item> Items { get; }
	public List<Fabric> Fabrics { get; }

	public Outfit(string label, List<Item> items, List<Fabric> fabrics) {
		Label = label;
		Items = items;
		Fabrics = fabrics;
	}
}
