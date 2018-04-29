using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		[JsonProperty(PropertyName = "visible-products")]
		public string[] visibleProducts;
	}
		
	public static ImportProperties Load(ImporterPathManager pathManager, string figureName) {
		JsonSerializerSettings settings = new JsonSerializerSettings {
			MissingMemberHandling = MissingMemberHandling.Error
		};
		string json = pathManager.GetConfDirForFigure(figureName).File("import-properties.json").ReadAllText();
		JsonProxy proxy = JsonConvert.DeserializeObject<JsonProxy>(json, settings);
		UrisJsonProxy urisProxy = proxy.uris;
		
		var uris = new FigureUris(
			urisProxy.basePath,
			urisProxy.documentName,
			urisProxy.geometryId,
			urisProxy.rootNodeId,
			urisProxy.skinBindingId);

		var visibleProducts = proxy.visibleProducts?.ToImmutableHashSet();
		
		return new ImportProperties(uris, proxy.hdCorrectionInitialValue, visibleProducts);
	}

	public FigureUris Uris { get; }
	public double HdCorrectionInitialValue { get; }
	public ImmutableHashSet<string> VisibleProducts { get; }

	public ImportProperties(FigureUris uris, double hdCorrectionInitialValue, ImmutableHashSet<string> visibleProducts) {
		Uris = uris;
		HdCorrectionInitialValue = hdCorrectionInitialValue;
		VisibleProducts = visibleProducts;
	}
}
