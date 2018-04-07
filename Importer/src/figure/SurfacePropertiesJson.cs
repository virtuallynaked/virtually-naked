using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

public class SurfacePropertiesJson {
	[Serializable]
	public class JsonProxy {
		[JsonProperty(PropertyName = "subdivision-level", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(0)]
		public int subdivisionLevel;

		[JsonProperty(PropertyName = "render-order")]
		public string[] renderOrder;

		[JsonProperty(PropertyName = "default-opacity", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(1)]
		public float defaultOpacity;

		[JsonProperty(PropertyName = "opacities")]
		public Dictionary<string, float> opacities;

		[JsonProperty(PropertyName = "precompute-scattering", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(false)]
		public bool precomputeScattering;

		[JsonProperty(PropertyName = "share-textures")]
		[DefaultValue(null)]
		public string shareTextures;

		[JsonProperty(PropertyName = "material-set-for-opacities")]
		[DefaultValue(null)]
		public string materialSetForOpacities;
	}

	public static SurfaceProperties Load(Figure figure) {
		JsonSerializerSettings settings = new JsonSerializerSettings {
			MissingMemberHandling = MissingMemberHandling.Error
		};
		string json = CommonPaths.ConfDir.Subdirectory(figure.Name).File("surface-properties.json").ReadAllText();
		JsonProxy proxy = JsonConvert.DeserializeObject<JsonProxy>(json, settings);

		int subdivisionLevel = proxy.subdivisionLevel;

		string[] surfaceNames = figure.Geometry.SurfaceNames;
		Dictionary<string, int> surfaceNameToIdx = Enumerable.Range(0, surfaceNames.Length)
			.ToDictionary(idx => surfaceNames[idx], idx => idx);

		int[] renderOrder;
		if (proxy.renderOrder == null) {
			renderOrder = new int[] { };
		} else {
			renderOrder = proxy.renderOrder.Select(surfaceName => surfaceNameToIdx[surfaceName]).ToArray();
		}

		float[] opacities = surfaceNames.Select(name => proxy.defaultOpacity).ToArray();
		if (proxy.opacities != null) {
			foreach (var entry in proxy.opacities) {
				opacities[surfaceNameToIdx[entry.Key]] = entry.Value;
			}
		}

		bool precomputeScattering = proxy.precomputeScattering;

		string shareTextures = proxy.shareTextures;

		string materialSetForOpacities = proxy.materialSetForOpacities;

		return new SurfaceProperties(subdivisionLevel, renderOrder, opacities, precomputeScattering, shareTextures, materialSetForOpacities);
	}
}
