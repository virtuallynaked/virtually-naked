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
		var file = figureConfDir.File("materials-sets.json");
		if (!file.Exists) {
			return new MaterialSetImportConfiguration[0];
		}
		string json = file.ReadAllText();
		MaterialSetImportConfiguration[] confs = JsonConvert.DeserializeObject<MaterialSetImportConfiguration[]>(json);
		return confs;
	}

	public string name;

	[JsonProperty("paths")]
	public string[] materialsDufPaths;

	[JsonProperty("post-paths")]
	public string[] postMaterialsDufPaths = { };

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

	[Serializable]
	public class Normals {
		[JsonProperty("surface-groups")]
		public List<List<String>> surfaceGroups;

		[JsonProperty("uv-set")]
		public string uvSet;

		[JsonProperty("textures")]
		public List<String> textures;

		[JsonProperty("generate-from-hd")]
		public bool generatedFromHd;
	}

	public string name;
	public Dictionary<string, double> morphs = new Dictionary<string, double>();

	[JsonProperty("parent-overrides")]
	public Dictionary<string, double> parentOverrides = new Dictionary<string, double>();

	[JsonProperty("normals")]
	public Normals normals;
}
