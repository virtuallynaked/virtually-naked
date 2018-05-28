using Newtonsoft.Json;
using System.IO;

public class CharacterJsonProxy {
	[JsonProperty(PropertyName = "label")]
	public string label;

	[JsonProperty(PropertyName = "shape")]
	public string shape;

	[JsonProperty(PropertyName = "material-set")]
	public string materialSet;
}

public class CharacterImporter {
	public static void Import(ImporterPathManager pathManager, FileInfo characterConfFile, DirectoryInfo contentDestDirectory) {
		string name = characterConfFile.GetNameWithoutExtension();

		var charactersDir = contentDestDirectory.Subdirectory("characters");
		var destinationFile = charactersDir.File(name + ".dat");
		if (destinationFile.Exists) {
			return;
		}

		string json = characterConfFile.ReadAllText();
		var proxy = JsonConvert.DeserializeObject<CharacterJsonProxy>(json);
		var character = new Character(proxy.label, proxy.shape, proxy.materialSet);

		charactersDir.CreateWithParents();
		Persistance.Save(destinationFile, character);
	}
}
