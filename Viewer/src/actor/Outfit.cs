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

	public static Outfit BandeauBikiniOutfit = new Outfit("Bandeau Bikini", new List<Item> {
		new Item("bandeau-bikini-top", "Top", true),
		new Item("bandeau-bikini-bottoms", "Bottoms", true)
	});
	public static Outfit BreakfastInBedOutfit = new Outfit("Breakfast In Bed Pajamas", new List<Item> {
		new Item("breakfast-in-bed-tank", "Tank", true),
		new Item("breakfast-in-bed-shorts", "Shorts", true)
	});
	public static Outfit RelaxedSundayOutfit = new Outfit("Relaxed Sunday", new List<Item> {
		new Item("relaxed-sunday-tank", "Tank", true),
		new Item("relaxed-sunday-shorts", "Shorts", true),
		new Item("relaxed-sunday-shoes", "Shoes", true)
	});
	public static Outfit SummerDressSetOutfit = new Outfit("Summer Dress Set", new List<Item> {
		new Item("summer-dress-dress", "Dress", true),
		new Item("summer-dress-shoes", "Shoes", true)
	});
	public static Outfit UpscaleShopperOutfit = new Outfit("Upscale Shopper", new List<Item> {
		new Item("upscale-shopper-blazer", "Blazer", true),
		new Item("upscale-shopper-blouse", "Blouse", true),
		new Item("upscale-shopper-pants", "Pants", true),
		new Item("upscale-shopper-heels", "Heels", true)
	});
	public static Outfit SweetHomeOutfit = new Outfit("Sweet Home", new List<Item> {
		new Item("sweet-home-tshirt", "T-Shirt", true),
		new Item("sweet-home-panties", "Panties", true),
		new Item("sweet-home-shorts", "Shorts", false),
		new Item("sweet-home-thigh-socks", "Thigh Socks", true),
		new Item("sweet-home-shin-socks", "Shin Socks", false),
		new Item("sweet-home-ankle-socks", "Ankle Socks", false)
	});
	public static Outfit Naked = new Outfit("Naked", new List<Item> { });

	public static List<Outfit> Outfits = new List<Outfit> {
		BandeauBikiniOutfit,
		BreakfastInBedOutfit,
		RelaxedSundayOutfit,
		SummerDressSetOutfit,
		UpscaleShopperOutfit,
		SweetHomeOutfit,
		Naked
	};
	
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
