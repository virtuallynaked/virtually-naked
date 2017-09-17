using Newtonsoft.Json;
using System;
using System.ComponentModel;

public class ImportProperties {
	[Serializable]
	public class UrisJsonProxy {
		[JsonProperty(PropertyName = "base-path")]
		public string basePath;

		[JsonProperty(PropertyName = "document-name")]
		public string documentName;

		[JsonProperty(PropertyName = "root-node-id")]
		public string rootNodeId;

		[JsonProperty(PropertyName = "geometry-id")]
		public string geometryId;

		[JsonProperty(PropertyName = "skin-binding-id")]
		public string skinBindingId;
	}

	[Serializable]
	public class JsonProxy {
		[JsonProperty(PropertyName = "uris")]
		public UrisJsonProxy uris;

		[JsonProperty(PropertyName = "hd-correction-initial-value", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(0)]
		public double hdCorrectionInitialValue;
	}
		
	public static ImportProperties Load(string figureName) {
		JsonSerializerSettings settings = new JsonSerializerSettings {
			MissingMemberHandling = MissingMemberHandling.Error
		};
		string json = CommonPaths.ConfDir.Subdirectory(figureName).File("import-properties.json").ReadAllText();
		JsonProxy proxy = JsonConvert.DeserializeObject<JsonProxy>(json, settings);
		UrisJsonProxy urisProxy = proxy.uris;
		
		var uris = new FigureUris(
			urisProxy.basePath,
			urisProxy.documentName,
			urisProxy.geometryId,
			urisProxy.rootNodeId,
			urisProxy.skinBindingId);
		
		return new ImportProperties(uris, proxy.hdCorrectionInitialValue);
	}

	public FigureUris Uris { get; }
	public double HdCorrectionInitialValue { get; }

	public ImportProperties(FigureUris uris, double hdCorrectionInitialValue) {
		Uris = uris;
		HdCorrectionInitialValue = hdCorrectionInitialValue;
	}
}
