using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class MaterialSetImportConfiguration {
	[Serializable]
	public class Variant {
		[JsonProperty("name")]
		public string name;

		[JsonProperty("paths")]
		public string[] materialsDufPaths;
	}

	[Serializable]
	public class VariantCategory {
		[JsonProperty("name")]
		public string name;

		[JsonProperty("surfaces")]
		public string[] surfaces;

		[JsonProperty("variants")]
		public Variant[] variants;
	}

	public static MaterialSetImportConfiguration[] Load(DirectoryInfo figureConfDir) {
		string json = figureConfDir.File("materials-sets.json").ReadAllText();
		MaterialSetImportConfiguration[] confs = JsonConvert.DeserializeObject<MaterialSetImportConfiguration[]>(json);
		return confs;
	}

	public string name;

	[JsonProperty("paths")]
	public string[] materialsDufPaths;

	[JsonProperty("use-custom-occlusion")]
	public bool useCustomOcclusion;

	[JsonProperty("variant-categories")]
	public VariantCategory[] variantCategories = { };
}

[Serializable]
public class ShapeImportConfiguration {
	public static ShapeImportConfiguration[] Load(DirectoryInfo figureConfDir) {
		var shapesFile = figureConfDir.File("shapes.json");
		if (shapesFile.Exists) {
			string json = shapesFile.ReadAllText();
			ShapeImportConfiguration[] confs = JsonConvert.DeserializeObject<ShapeImportConfiguration[]>(json);
			return confs;
		} else {
			return new ShapeImportConfiguration[] {};
		}
	}

	public string name;
	public Dictionary<string, double> morphs = new Dictionary<string, double>();

	[JsonProperty("parent-overrides")]
	public Dictionary<string, double> parentOverrides = new Dictionary<string, double>();
}
