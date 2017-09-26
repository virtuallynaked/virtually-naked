using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

class ScenePersistenceMenuLevel : IMenuLevel {
	public static ScenePersistenceMenuLevel Make(Scene scene) {
		var savesDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Subdirectory("saves");
		if (!savesDirectory.Exists) {
			savesDirectory.Create();
		}
		return new ScenePersistenceMenuLevel(scene, savesDirectory);
	}
	
	private readonly Scene scene;
	private readonly DirectoryInfo savesDirectory;

	public ScenePersistenceMenuLevel(Scene scene, DirectoryInfo savesDirectory) {
		this.scene = scene;
		this.savesDirectory = savesDirectory;
	}

	private void LoadScene(FileInfo sceneFile) {
		using (var textReader = sceneFile.OpenText())
		using (var jsonReader = new JsonTextReader(textReader)) {
			var recipe = JsonSerializer.Create().Deserialize<Scene.Recipe>(jsonReader);
			recipe.Merge(scene);
		}
	}

	private void SaveScene() {
		var recipe = scene.Recipize();
		
		var sceneFile = savesDirectory.File(Guid.NewGuid().ToString() + ".scene");
		using (var textWriter = sceneFile.CreateText())
		using (var jsonWriter = new JsonTextWriter(textWriter)) {
			var serializerSettings = new JsonSerializerSettings {
				Formatting = Formatting.Indented
			};
			JsonSerializer.Create(serializerSettings).Serialize(jsonWriter, recipe);
		}
	}
	
	public List<IMenuItem> GetItems() {
		List<IMenuItem> items = new List<IMenuItem> { };
		
		items.Add(new ActionMenuItem("Save", () => SaveScene()));

		foreach (var sceneFile in savesDirectory.EnumerateFiles("*.scene")) {
			string name = sceneFile.GetNameWithoutExtension();
			items.Add(new ActionMenuItem(name, () => LoadScene(sceneFile)));
		}
		return items;
	}
}