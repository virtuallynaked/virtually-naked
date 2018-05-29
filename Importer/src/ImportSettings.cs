using System.Collections.Generic;
using System.Linq;

public class ImportSettings {
	public static ImportSettings MakeReleaseSettings() {
		var ReleaseCharacterShapes = new HashSet<string> {
			"Mei Lin", "Rune", "Eva", "Monique", "Victoria", "Kalea",
			"Lien", "Soraya", "Phoenix", "Halina" };
		var ReleaseCharactersSkins = new HashSet<string> { "Mei Lin", "Rune", "Eva", "Monique", "Victoria", "Kalea" };

		return new ImportSettings {
			CompressTextures = true,

			Environments = {
				"Maui",
				"Outdoor A",
				"Outdoor B",
				"Room",
				"Ruins A",
				"Ruins B",
				"Ruins C",
				"Studio",
				"Studio",
				"Sunset"
			},

			Figures = new Dictionary<string, FigureImportSettings> {
				["genesis-3-female"] = new FigureImportSettings {
					Shapes = ReleaseCharacterShapes,
					MaterialSets = ReleaseCharactersSkins
				},

				["liv-hair"] = FigureImportSettings.All,

				["breakfast-in-bed-tank"] = FigureImportSettings.All,
				["breakfast-in-bed-shorts"] = FigureImportSettings.All,

				["bandeau-bikini-top"] = FigureImportSettings.All,
				["bandeau-bikini-bottoms"] = FigureImportSettings.All,

				["relaxed-sunday-tank"] = FigureImportSettings.All,
				["relaxed-sunday-shorts"] = FigureImportSettings.All,
				["relaxed-sunday-shoes"] = FigureImportSettings.All,

				["summer-dress-dress"] = FigureImportSettings.All,
				["summer-dress-shoes"] = FigureImportSettings.All,

				["upscale-shopper-blazer"] = FigureImportSettings.All,
				["upscale-shopper-blouse"] = FigureImportSettings.All,
				["upscale-shopper-pants"] = FigureImportSettings.All,
				["upscale-shopper-heels"] = FigureImportSettings.All,

				["sweet-home-tshirt"] = FigureImportSettings.All,
				["sweet-home-panties"] = FigureImportSettings.All,
				["sweet-home-shorts"] = FigureImportSettings.All,
				["sweet-home-thigh-socks"] = FigureImportSettings.All,
				["sweet-home-shin-socks"] = FigureImportSettings.All,
				["sweet-home-ankle-socks"] = FigureImportSettings.All,
			}
		};
	}

	public static ImportSettings MakeFromViewerInitialSettings() {
		List<string> figureNames = new List<string>() { };
		figureNames.Add(InitialSettings.Main);
		if (InitialSettings.Hair != null) {
			figureNames.Add(InitialSettings.Hair);
		}
		figureNames.AddRange(InitialSettings.Clothing);
		
		return new ImportSettings {
			CompressTextures = false,
			Environments = { InitialSettings.Environment },
			Figures = figureNames.ToDictionary(
				name => name,
				name => FigureImportSettings.MakeFromViewInitialSettings(name))
		};
	}

	public bool CompressTextures { get; set; } = false;

	private HashSet<string> Environments { get; set; } = new HashSet<string>();
	
	private Dictionary<string, FigureImportSettings> Figures { get; set; } = new Dictionary<string, FigureImportSettings>();
	
	public bool ShouldImportEnvironment(string name) {
		return name == InitialSettings.Environment;
	}

	public IEnumerable<string> FiguresToImport => Figures.Keys;

	public bool ShouldImportShape(string figureName, string shapeName) {
		var figureSettings = Figures[figureName];
		var shapes = figureSettings.Shapes;
		if (shapes == null) {
			return shapeName != "Base";
		} else {
			return shapes.Contains(shapeName);
		}
	}

	public bool ShouldImportMaterialSet(string figureName, string materialSetName) {
		var figureSettings = Figures[figureName];
		var materialSets = figureSettings.MaterialSets;
		if (materialSets == null) {
			return materialSetName != "Base";
		} else {
			return materialSets.Contains(materialSetName);
		}
	}
}

public class FigureImportSettings {
	public static FigureImportSettings MakeFromViewInitialSettings(string figure) {
		HashSet<string> shapes;
		if (InitialSettings.Shapes.TryGetValue(figure, out string initialShape)) {
			shapes = new HashSet<string> { initialShape };
		} else {
			shapes = new HashSet<string> { };
		}

		string initialMaterialSet = InitialSettings.MaterialSets[figure];
			
		return new FigureImportSettings {
			Shapes = shapes,
			MaterialSets = { initialMaterialSet }
		};
	}

	public static FigureImportSettings All => new FigureImportSettings {
		Shapes = null,
		MaterialSets = null
	};

	public HashSet<string> Shapes { get; set; } = new HashSet<string>(); // null means all except "Base"
	public HashSet<string> MaterialSets { get; set; } = new HashSet<string>(); // null means all
}
