using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class MaterialSetImportConfiguration {
	public static MaterialSetImportConfiguration[] Load(string figureName) {
		string json = CommonPaths.ConfDir.Subdirectory(figureName).File("materials-sets.json").ReadAllText();
		MaterialSetImportConfiguration[] confs = JsonConvert.DeserializeObject<MaterialSetImportConfiguration[]>(json);
		return confs;
	}

	public string name;

	[JsonProperty("paths")]
	public string[] materialsDufPaths;

	[JsonProperty("use-custom-occlusion")]
	public bool useCustomOcclusion;
}

[Serializable]
public class ShapeImportConfiguration {
	public static ShapeImportConfiguration[] Load(string figureName) {
		var shapesFile = CommonPaths.ConfDir.Subdirectory(figureName).File("shapes.json");
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
