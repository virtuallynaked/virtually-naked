using Newtonsoft.Json.Linq;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;

public class RawImageInfo {
	public FileInfo file;
	public float gamma;
}

public class RawColorTexture {
	public Vector3 value;
	public RawImageInfo image;
}

public class RawFloatTexture {
	public float value;
	public RawImageInfo image;
}

public class MaterialBag {
	public const string IrayUberType = "studio/material/uber_iray";
	public const string DazBrickType = "studio/material/daz_brick";

	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly Dictionary<string, DsonTypes.Image> imagesByUrl = new Dictionary<string, DsonTypes.Image>();
	private readonly HashSet<string> extraTypes = new HashSet<string>();
	private readonly Dictionary<string, object> values = new Dictionary<string, object>();
	
	public MaterialBag(ContentFileLocator fileLocator, DsonObjectLocator objectLocator, Dictionary<string, DsonTypes.Image> imagesByUrl) {
		this.fileLocator = fileLocator;
		this.objectLocator = objectLocator;
		this.imagesByUrl = imagesByUrl;
	}

	public void AddExtraType(string type) {
		extraTypes.Add(type);
	}

	public bool HasExtraType(string type) {
		return extraTypes.Contains(type);
	}

	private object GetValue(string channelName, string propertyName) {
		string url = Uri.EscapeUriString(channelName) + "/" + propertyName;
		values.TryGetValue(url, out object val);
		return val;
	}

	private object GetValue(string propertyName) {
		string url = Uri.EscapeUriString(propertyName);
		values.TryGetValue(url, out object val);
		return val;
	}

	public void SetValue(string channelName, string propertyName, object val) {
		if (val == null) {
			return;
		}

		string url = Uri.EscapeUriString(channelName) + "/" + propertyName;
		values[url] = val;
	}

	public void SetValue(string propertyName, object val) {
		if (val == null) {
			return;
		}

		string url = Uri.EscapeUriString(propertyName);
		values[url] = val;
	}

	public void RemoveByUrl(string url) {
		values.Remove(url);
	}

	public void SetByUrl(string url, object val) {
		values[url] = val;
	}

	
	private object GetNonNullValue(string channelName, string propertyName) {
		object value = GetValue(channelName, propertyName);
		if (value == null) {
			throw new NullReferenceException();
		}
		return value;
	}
		
	public int ExtractInteger(string channelName) {
		return Convert.ToInt32(GetNonNullValue(channelName, "value"));
	}

	public bool ExtractBoolean(string channelName) {
		return Convert.ToBoolean(GetNonNullValue(channelName, "value"));
	}

	public float ExtractFloat(string channelName) {
		return Convert.ToSingle(GetNonNullValue(channelName, "value"));
	}

	private float ExtractScale(string channelName) {
		object val = GetValue(channelName, "image_modification/scale");
		return val != null ? Convert.ToSingle(val) : 1f;
	}

	public RawImageInfo ExtractImage(string channelName) {
		string imageUrl = (string) GetValue(channelName, "image_file");

		if (imageUrl == null) {
			return null;
		}
		
		if (!imagesByUrl.TryGetValue(imageUrl, out var image)) {
			//use a default setting
			image = new DsonTypes.Image {
				map_gamma = 0
			};
		}
		
		string texturePath = Uri.UnescapeDataString(imageUrl);
		FileInfo textureFile = new FileInfo(fileLocator.Locate(texturePath));

		return new RawImageInfo {
			file = textureFile,
			gamma = image.map_gamma
		};
	}
	
	public Vector3 ExtractColor(string channelName) {
		Vector3 color = new Vector3();

		object val = GetValue(channelName, "value");
		switch (val) {
			case JArray arrayValue:
				float[] values = arrayValue.ToObject<float[]>();
				color[0] = values[0];
				color[1] = values[1];
				color[2] = values[2];
				break;

			case null:
				color = Vector3.Zero;
				break;

			default:
				throw new InvalidOperationException("expected a color:" + channelName);
		}

		color = ColorUtils.SrgbToLinear(color);

		return color;
	}
		
	public RawColorTexture ExtractColorTexture(string channelName) {
		var texture = new RawColorTexture {
			image = ExtractImage(channelName),
			value = ExtractColor(channelName) * ExtractScale(channelName)
		};
		return texture;
	}

	public RawFloatTexture ExtractFloatTexture(string channelName) {
		var texture = new RawFloatTexture {
			image = ExtractImage(channelName),
			value = ExtractFloat(channelName) * ExtractScale(channelName)
		};
		return texture;
	}

	public string ExtractUvSetName(Figure figure) {
		object uvUrl = GetValue( "uv_set");
		string uvName;
		if (uvUrl.Equals(0L)) {
			uvName = figure.DefaultUvSet.Name;
		} else {
			uvName = objectLocator.Locate((string) uvUrl).name;

			if (!figure.UvSets.TryGetValue(uvName, out var ignore)) {
				if (uvName == "default") {
					//hack to handle uv-set for genital graft
					uvName = figure.DefaultUvSet.Name;
				} else {
					throw new InvalidOperationException("unrecognized UV set: " + uvName);
				}
			}
		}

		return uvName;
	}
}
