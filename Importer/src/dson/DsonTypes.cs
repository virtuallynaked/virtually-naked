using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;

namespace DsonTypes {
	public class DsonDocument {
		private DsonRoot root;

		public DsonObjectLocator Locator {get; }
		public string BaseUri {get; }
		public DsonRoot Root => root;

		private DsonDocument(DsonObjectLocator locator, string documentPath) {
			Locator = locator;
			BaseUri = documentPath;
		}
				
        public static DsonDocument LoadFromFile(DsonObjectLocator locator, string contentPath, string documentPath) {
            using (StreamReader reader = File.OpenText(contentPath)) {
                using (JsonReader jsonReader = new JsonTextReader(reader)) {
					DsonDocument document = new DsonDocument(locator, documentPath);

					var serializer = JsonSerializer.CreateDefault();
					serializer.Converters.Add(new DsonObjectReferenceConverter(document));
					DsonRoot root = serializer.Deserialize<DsonRoot>(jsonReader);

					document.root = root;

					return document;
                }
            }
        }

		public string ResolveUri(string uri) {
			if (uri.StartsWith("#")) {
				return BaseUri + uri;
			} else if (uri.StartsWith("/")) {
				return uri;
			} else {
				throw new InvalidOperationException("unrecognized uri style: " + uri);
			}
		}
	}

	public class DsonObjectReference<T> where T : DsonObject {
		public DsonDocument Document {get; }
		public string RelativeUri {get; }

		public DsonObjectReference(DsonDocument document, string relativeUri) {
			Document = document;
			RelativeUri = relativeUri ?? throw new ArgumentNullException("relativeUri");
		}

		public string Uri {
			get {
				return Document.ResolveUri(RelativeUri);
			}
		}

		public T LocateReferencedObject(bool throwIfMissing) {
			 return (T) Document.Locator.Locate(Uri, throwIfMissing);
		}

		public T ReferencedObject {
			get {
				return LocateReferencedObject(true);
			}
		}

	}

	class DsonObjectReferenceConverter : JsonConverter {
		private readonly DsonDocument document;

		public DsonObjectReferenceConverter(DsonDocument document) {
			this.document = document;
		}

		public override bool CanConvert(Type objectType) {
			return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(DsonObjectReference<>);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType != JsonToken.String) {
				throw new InvalidOperationException("expected a string token");
			}
			string relativeUri = (string) reader.Value;
			return objectType.GetConstructors()[0].Invoke(new object[] { document, relativeUri });
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}
	
	[Serializable]
    public class DsonRoot {
        public string file_version;
        public AssetInfo asset_info;

        public Geometry[] geometry_library;
        public Modifier[] modifier_library;
        public UvSet[] uv_set_library;
        public Node[] node_library;
        public Image[] image_library;
		public Material[] material_library;

        public Scene scene;
    }

    [Serializable]
    public class DsonObject {
        public string id;
		public string url;
		public string name;
		public string label;
    }

    [Serializable]
    public class Image : DsonObject {
        public float map_gamma;
        public ImageMap[] map;
    }

    [Serializable]
    public class ImageMap {
        public string url;
    }

    [Serializable]
    public class Scene {
        public ModifierInstance[] modifiers;
		public MaterialInstance[] materials;
        public ChannelAnimation[] animations;
    }

    [Serializable]
    public class ChannelAnimation {
        public string url;
        public Object[][] keys;
    }

    [Serializable]
    public class CountedArray<T> {
        public int count;
        public T[] values;
    }

    [Serializable]
    public class UvSet : DsonObject {
        public int vertex_count;
        public CountedArray<float[]> uvs;
        public int[][] polygon_vertex_indices;

    }

    [Serializable]
    public class AssetInfo {
        public string id;
        public string type;
    }

	
	class GeometryTypeConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return true;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType != JsonToken.String) {
				throw new InvalidOperationException("expected operator to be a string");
			}

			string value = (string) reader.Value;

			if (value == "polygon_mesh") {
				return GeometryType.PolygonMesh;
			} else if (value == "subdivision_surface") {
				return GeometryType.SubdivisionSurface;
			} else {
				throw new InvalidOperationException("unrecognized operator: " + value);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}

	[JsonConverter(typeof(GeometryTypeConverter))]
	public enum GeometryType {
		PolygonMesh, SubdivisionSurface
	}

    [Serializable]
    public class Geometry : DsonObject {
		public GeometryType type;
		public CountedArray<string> polygon_groups;
        public CountedArray<string> polygon_material_groups;
        public Float3Array vertices;
        public PolygonArray polylist;
		public DsonObjectReference<UvSet> default_uv_set;
        public Graft graft;
    }

    [Serializable]
    public class Graft {
        public Int2DArray vertex_pairs;
        public IntArray hidden_polys;
    }

    [Serializable]
    public class IntArray {
        public int count;
        public int[] values;
    }

    [Serializable]
    public class Int2DArray {
        public int count;
        public int[][] values;
    }

    [Serializable]
    public class Float3Array {
        public int count;
        public float[][] values;
    }

    [Serializable]
    public class PolygonArray {
        public int count;
        public int[][] values;
    }

    [Serializable]
    public class Presentation {
        public string type;
    }

    [Serializable]
    public class Modifier : DsonObject {
        public string source;
        public DsonObjectReference<DsonObject> parent;
        public Presentation presentation;
        public ChannelFloat channel;
		public string region;
        public string group;
        public SkinBinding skin;
        public Morph morph;
        public Formula[] formulas;
    }

    [Serializable]
    public class Channel : DsonObject {
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool visible;

        public bool locked;
		
        public double value;
        public double current_value;

        public double min;
        public double max;
        public bool clamped;

		public string target_channel;
    }

    [Serializable]
    public class ChannelFloat : Channel {

    }

	class IndexedFloatConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			IndexedFloat obj = default(IndexedFloat);
			obj.index = reader.ReadAsInt32().Value;
			obj.value = reader.ReadAsDouble().Value;
			reader.Read();
			if (reader.TokenType != JsonToken.EndArray) {
				throw new JsonSerializationException("expected end of array");
			}
			return obj;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}

	[Serializable]
	[JsonConverter(typeof(IndexedFloatConverter))]
	public struct IndexedFloat {
		public int index;
		public double value;
	}

	[Serializable]
	public class WeightedJoint {
		public DsonObjectReference<Node> node;
		public CountedArray<IndexedFloat> node_weights;

		//these only exist to ensure they're not set
		public object scale_weights;
		public object local_weights;
		public object bulge_weights;
	}

	
	class StringPairConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			StringPair obj = default(StringPair);
			obj.from = reader.ReadAsString();
			obj.to = reader.ReadAsString();
			reader.Read();
			if (reader.TokenType != JsonToken.EndArray) {
				throw new JsonSerializationException("expected end of array");
			}
			return obj;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}

	[Serializable]
	[JsonConverter(typeof(StringPairConverter))]
	public struct StringPair {
		public string from;
		public string to;
	}

	[Serializable]
	public class NamedStringMap {
		public string id;
		public StringPair[] mappings;
	}

	[Serializable]
    public class SkinBinding {
		public int vertex_count;
		public WeightedJoint[] joints;
		public NamedStringMap[] selection_map;
    }

    [Serializable]
    public class Morph {
        public int vertex_count;
        public Float3Array deltas;
        public string hd_url;
    }

	class StageConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return true;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType != JsonToken.String) {
				throw new InvalidOperationException("expected Stage to be a string");
			}

			string value = (string) reader.Value;

			if (value == "multiply" || value == "mult") {
				return Stage.Multiply;
			} else if (value == "sum") {
				return Stage.Sum;
			} else {
				throw new InvalidOperationException("unrecognized stage: " + value);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}

	[JsonConverter(typeof(StageConverter))]
	public enum Stage {
		Sum,
		Multiply
	}

    [Serializable]
    public class Formula {
        public string output;
        public Operation[] operations;
        public Stage stage;
    }

	class OperatorConverter : JsonConverter {
		public override bool CanConvert(Type objectType) {
			return true;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType != JsonToken.String) {
				throw new InvalidOperationException("expected operator to be a string");
			}

			string value = (string) reader.Value;

			if (value == "push") {
				return Operator.Push;
			} else if (value == "add") {
				return Operator.Add;
			} else if (value == "sub") {
				return Operator.Sub;
			} else if (value == "mult") {
				return Operator.Mult;
			} else if (value == "div") {
				return Operator.Div;
			} else if (value == "spline_tcb") {
				return Operator.SplineTcb;
			} else {
				throw new InvalidOperationException("unrecognized operator: " + value);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}
	}

	[JsonConverter(typeof(OperatorConverter))]
	public enum Operator {
		Push, Add, Sub, Mult, Div, SplineTcb
	}

    [Serializable]
    public class Operation {
        public Operator op;
        public Object val;
        public string url;
    }

    [Serializable]
    public class Node : DsonObject {
        public string type;
        public DsonObjectReference<Node> parent;
        public string rotation_order;

		[DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public bool inherits_scale;

        public ChannelFloat[] center_point;
        public ChannelFloat[] end_point;
        public ChannelFloat[] orientation;
        public ChannelFloat[] rotation;
        public ChannelFloat[] translation;
		public ChannelFloat[] scale;
		public ChannelFloat general_scale;
        public Formula[] formulas;
    }

    [Serializable]
    public class ModifierInstance {
        public string id;
        public string url;
    }

	[Serializable]
	public class Material : DsonObject {
		public string uv_set;
		public MaterialChannel diffuse;
		public MaterialExtra[] extra;
	}

	[Serializable]
	public class MaterialChannel {
		public MaterialChannelChannel channel;
	}

	[Serializable]
	public class MaterialChannelChannel {
		public string id;
		public object current_value;
		public string image_file;
		public ImageModification image_modification;
	}

	[Serializable]
	public class ImageModification {
		public float scale;
	}

	[Serializable]
	public class MaterialExtra {
		public string type;
		public MaterialChannel[] channels;
	}

	[Serializable]
	public class MaterialInstance {
		public DsonObjectReference<Material> url;

		public string uv_set;
		public MaterialChannel diffuse;
		public MaterialExtra[] extra;
		public string[] groups;
	}
}
