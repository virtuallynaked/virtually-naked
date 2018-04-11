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

	public static Outfit Naked = new Outfit("Naked", new List<Item> { });

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

	public Outfit(string label, List<Item> items) {
		Label = label;
		Items = items;
	}
}
