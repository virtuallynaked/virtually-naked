using System.Collections.Generic;
using System.Linq;

public class Outfit {
	public class OutfitElement {
		public string Figure { get; }
		public string Label { get; }
		public bool IsInitiallyVisible { get; }

		public OutfitElement(string figure, string label, bool isInitiallyVisible) {
			Figure = figure;
			Label = label;
			IsInitiallyVisible = isInitiallyVisible;
		}
	}

	public static Outfit BandeauBikiniOutfit = new Outfit("Bandeau Bikini", new List<OutfitElement> {
		new OutfitElement("bandeau-bikini-top", "Top", true),
		new OutfitElement("bandeau-bikini-bottoms", "Bottoms", true)
	});
	public static Outfit BreakfastInBedOutfit = new Outfit("Breakfast In Bed Pajamas", new List<OutfitElement> {
		new OutfitElement("breakfast-in-bed-tank", "Tank", true),
		new OutfitElement("breakfast-in-bed-shorts", "Shorts", true)
	});
	public static Outfit RelaxedSundayOutfit = new Outfit("Relaxed Sunday", new List<OutfitElement> {
		new OutfitElement("relaxed-sunday-tank", "Tank", true),
		new OutfitElement("relaxed-sunday-shorts", "Shorts", true),
		new OutfitElement("relaxed-sunday-shoes", "Shoes", true)
	});
	public static Outfit SummerDressSetOutfit = new Outfit("Summer Dress Set", new List<OutfitElement> {
		new OutfitElement("summer-dress-dress", "Dress", true),
		new OutfitElement("summer-dress-shoes", "Shoes", true)
	});
	public static Outfit UpscaleShopperOutfit = new Outfit("Upscale Shopper", new List<OutfitElement> {
		new OutfitElement("upscale-shopper-blazer", "Blazer", true),
		new OutfitElement("upscale-shopper-blouse", "Blouse", true),
		new OutfitElement("upscale-shopper-pants", "Pants", true),
		new OutfitElement("upscale-shopper-heels", "Heels", true)
	});
	public static Outfit SweetHomeOutfit = new Outfit("Sweet Home", new List<OutfitElement> {
		new OutfitElement("sweet-home-tshirt", "T-Shirt", true),
		new OutfitElement("sweet-home-panties", "Panties", true),
		new OutfitElement("sweet-home-shorts", "Shorts", false),
		new OutfitElement("sweet-home-thigh-socks", "Thigh Socks", true),
		new OutfitElement("sweet-home-shin-socks", "Shin Socks", false),
		new OutfitElement("sweet-home-ankle-socks", "Ankle Socks", false)
	});

	public static List<Outfit> Outfits = new List<Outfit> {
		BandeauBikiniOutfit,
		BreakfastInBedOutfit,
		RelaxedSundayOutfit,
		SummerDressSetOutfit,
		UpscaleShopperOutfit,
		SweetHomeOutfit
	};
	
	public bool IsMatch(FigureFacade[] figures) {
		var isMatch = figures.Select(figure => figure.Definition.Name)
			.SequenceEqual(Elements.Select(element => element.Figure));
		return isMatch;
	}

	public string Label { get; }
	public List<OutfitElement> Elements { get; }

	public Outfit(string label, List<OutfitElement> figures) {
		Label = label;
		Elements = figures;
	}
}
