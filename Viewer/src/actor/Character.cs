using System.Collections.Generic;
using System.Linq;

public class Character {
	public string Label { get; }
	public string Shape { get; }
	public string MaterialSet { get; }

	public Character(string label, string shape, string materialSet) {
		Label = label;
		Shape = shape;
		MaterialSet = materialSet;
	}
	
	public static List<Character> LoadList(IArchiveDirectory dataDir) {
		var dir = dataDir.Subdirectory("characters");
		var characters = dir.GetFiles()
			.Select(file => Persistance.Load<Character>(file))
			.ToList();
		return characters;
	}
}
