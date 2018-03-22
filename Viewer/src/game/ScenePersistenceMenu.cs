using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Valve.VR;

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

	public event Action ItemsChanged;

	private void LoadScene(FileInfo sceneFile) {
		using (var textReader = sceneFile.OpenText())
		using (var jsonReader = new JsonTextReader(textReader)) {
			var recipe = JsonSerializer.Create().Deserialize<Scene.Recipe>(jsonReader);
			recipe.Merge(scene);
		}
	}

	private void SaveScene(string name) {
		if (String.IsNullOrEmpty(name)) {
			return;
		}

		var recipe = scene.Recipize();
		
		var sceneFile = savesDirectory.File(name + ".scene");
		using (var textWriter = sceneFile.CreateText())
		using (var jsonWriter = new JsonTextWriter(textWriter)) {
			var serializerSettings = new JsonSerializerSettings {
				Formatting = Formatting.Indented
			};
			JsonSerializer.Create(serializerSettings).Serialize(jsonWriter, recipe);
		}

		ItemsChanged?.Invoke();
	}

	private void SaveScene() {
		OpenVRKeyboardHelper.PromptForString(
			EGamepadTextInputMode.k_EGamepadTextInputModeNormal,
			EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine,
			"Scene Name",
			0,
			"",
			SaveScene);
	}
	
	public List<IMenuItem> GetItems() {
		List<IMenuItem> items = new List<IMenuItem> { };
		
		items.Add(new ActionMenuItem("Save", () => SaveScene(), true));

		foreach (var sceneFile in savesDirectory.EnumerateFiles("*.scene")) {
			string name = sceneFile.GetNameWithoutExtension();
			items.Add(new ActionMenuItem(name, () => LoadScene(sceneFile)));
		}
		return items;
	}
}
