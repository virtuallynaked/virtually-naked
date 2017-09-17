using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ColladaTypes {
	public class Source {
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlElement("float_array")]
		public string FloatArray { get; set; }

		[XmlElement("Name_array")]
		public string NameArray { get; set; }
	}

	public class Animation {
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("source")]
		public List<Source> Sources { get; set; }
	}

	public class AnimationLibrary {
		[XmlElement("animation")]
		public List<Animation> Animations { get; set; }
	}

	public class Skin {
		[XmlElement("source")]
		public List<Source> Sources { get; set; }
	}

	public class Controller {
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlElement("skin")]
		public Skin Skin { get; set; }
	}

	public class ControllerLibrary {
		[XmlElement("controller")]
		public List<Controller> Controllers { get; set; }
	}

	public class Node {
		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("matrix")]
		public string Matrix { get; set; }

		[XmlElement("node")]
		public List<Node> Nodes { get; set; }
	}

	public class VisualScene {
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlElement("node")]
		public List<Node> Nodes { get; set; }
	}


	public class VisualSceneLibrary {
		[XmlElement("visual_scene")]
		public List<VisualScene> VisualScenes { get; set; }
	}

	[XmlRoot("COLLADA", Namespace="http://www.collada.org/2005/11/COLLADASchema")]
	public class ColladaRoot {
		[XmlElement("library_animations")]
		public AnimationLibrary LibraryAnimations { get; set; }

		[XmlElement("library_controllers")]
		public ControllerLibrary ControllerLibrary { get; set; }

		[XmlElement("library_visual_scenes")]
		public VisualSceneLibrary VisualSceneLibrary { get; set; }

		public static XmlSerializer Serializer = new XmlSerializer(typeof(ColladaRoot));
	}
}
