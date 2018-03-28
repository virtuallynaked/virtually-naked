using SharpDX;
using System.Collections.Generic;

public static class InitialSettings {
	public const string Environment = "Ruins B";

	public const string Character = "Eva";
	
	public static readonly Matrix InitialTransform = Matrix.Translation(0, 0, -0.5f) * Matrix.RotationY(0);

	public const string Animation = "idle";
	public const float AnimationSpeed = 0.8f;

	public static string Main = "genesis-3-female";

	public static string Hair =
		//null;
		//"chace-hair";
		//"krayon-hair";
		//"brady-hair";
		//"oleander-hair";
		//"lani-hair";
		//"leyton-hair";
		//"galene-hair";
		//"sapphire-hair";
		//"maia-hair";
		//"odessa-hair";
		//"ohara-hair";
		"liv-hair";
		//"lainey-hair";

	public static List<string> Clothing = new List<string> {
		//"tropical-style-bikini-top",
		//"tropical-style-bikini-bottom",
		//"tropical-style-sarong",
		//"tropical-style-sandals",
		//"maui-days-dress",
		//"sorbet-swimsuit"
		//"red-lilies-dress",
		//"red-lilies-robe",
		//"red-lilies-shorts"
		"breakfast-in-bed-tank",
		"breakfast-in-bed-shorts",
		//"trend-setter-shirt",
		//"trend-setter-pants",
		//"urban-edgy-top",
		//"urban-edgy-shorts",
		//"urban-edgy-jacket",
		//"urban-edgy-boots",
		//"mercurial-guardian-bodice",
		//"mercurial-guardian-boots",
		//"mercurial-guardian-pants",
		//"mercurial-guardian-shoulder-armor",
		//"mercurial-guardian-sleeves",
		//"bandeau-bikini-top",
		//"bandeau-bikini-bottoms",
		//"relaxed-sunday-tank",
		//"relaxed-sunday-shorts",
		//"relaxed-sunday-shoes",
		//"summer-dress-dress",
		//"summer-dress-shoes",
		//"upscale-shopper-blazer",
		//"upscale-shopper-blouse",
		//"upscale-shopper-pants",
		//"upscale-shopper-heels"
		//"sweet-home-tshirt",
		//"sweet-home-panties",
		//"sweet-home-shorts",
		//"sweet-home-thigh-socks",
		//"sweet-home-shin-socks",
		//"sweet-home-ankle-socks"
	};

	public static readonly Dictionary<string, string> Shapes = new Dictionary<string, string> {
		["genesis-3-female"] = Character,

		["liv-hair"] = "Style 02",
		["lainey-hair"] = "Style 01"
	};

	public static readonly Dictionary<string, string> MaterialSets = new Dictionary<string, string> {
		["genesis-3-female"] = Character,

		["chace-hair"] = "Blonde",
		["krayon-hair"] = "01",
		["brady-hair"] = "01",
		["oleander-hair"] = "01",
		["lani-hair"] = "Black 01",
		["leyton-hair"] = "Brown",
		["galene-hair"] = "01",
		["sapphire-hair"] = "01",
		["maia-hair"] = "Black",
		["odessa-hair"] = "Brown",
		["ohara-hair"] = "01",
		["liv-hair"] = "01",
		["lainey-hair"] = "01",

		["tropical-style-bikini-top"] = "Aloha",
		["tropical-style-bikini-bottom"] = "Aloha",
		["tropical-style-sarong"] = "Aloha",
		["tropical-style-sandals"] = "Aloha",
		["maui-days-dress"] = "01",
		["sorbet-swimsuit"] = "Style 1",
		["red-lilies-dress"] = "Nixishia",
		["red-lilies-robe"] = "Nixishia",
		["red-lilies-shorts"] = "Nixishia",
		["breakfast-in-bed-tank"] = "Azure Dream",
		["breakfast-in-bed-shorts"] = "Azure Dream",
		["trend-setter-shirt"] = "Lola April",
		["trend-setter-pants"] = "Lola April",
		["urban-edgy-top"] = "Green Camo",
		["urban-edgy-shorts"] = "Green Camo",
		["urban-edgy-jacket"] = "Green Camo",
		["urban-edgy-boots"] = "Green Camo",
		["mercurial-guardian-bodice"] = "Thalenia",
		["mercurial-guardian-boots"] = "Thalenia",
		["mercurial-guardian-pants"] = "Thalenia",
		["mercurial-guardian-shoulder-armor"] = "Thalenia",
		["mercurial-guardian-sleeves"] = "Thalenia",
		["bandeau-bikini-top"] = "Teal",
		["bandeau-bikini-bottoms"] = "Teal",
		["relaxed-sunday-tank"] = "Paisley",
		["relaxed-sunday-shorts"] = "Paisley",
		["relaxed-sunday-shoes"] = "Paisley",
		["summer-dress-dress"] = "Cream",
		["summer-dress-shoes"] = "Cream",
		["upscale-shopper-blazer"] = "Black",
		["upscale-shopper-blouse"] = "White Skull",
		["upscale-shopper-pants"] = "Black",
		["upscale-shopper-heels"] = "Black",
		["sweet-home-tshirt"] = "Chantly",
		["sweet-home-panties"] = "Chantly",
		["sweet-home-shorts"] = "Chantly",
		["sweet-home-thigh-socks"] = "Chantly",
		["sweet-home-shin-socks"] = "Chantly",
		["sweet-home-ankle-socks"] = "Chantly",
	};
}
