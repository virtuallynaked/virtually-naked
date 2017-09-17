using SharpDX.Direct3D11;
using System;
using System.Linq;

public class MaterialSet : IDisposable {
	public static MaterialSet LoadActive(Device device, ShaderCache shaderCache, IArchiveDirectory figureDir, string materialSetName) {
		var materialsDirectory = figureDir
			.Subdirectory("material-sets")
			.Subdirectory(materialSetName);

		bool hasSharedTextures = figureDir.Name.EndsWith("-hair"); //super hacky!
		var texturesDirectory = hasSharedTextures ? figureDir.Subdirectory("textures") : materialsDirectory;
		
		var textureLoader = new TextureLoader(device, texturesDirectory);
		var multiMaterialSettings = Persistance.Load<MultiMaterialSettings>(materialsDirectory.File("material-settings.dat"));
		var materials = multiMaterialSettings.PerMaterialSettings.Select(settings => settings.Load(device, shaderCache, textureLoader)).ToArray();
		return new MaterialSet(textureLoader, materials);
	}
	
	private readonly TextureLoader textureLoader;
	private IMaterial[] materials;

	public MaterialSet(TextureLoader textureLoader, IMaterial[] materials) {
		this.textureLoader = textureLoader;
		this.materials = materials;
	}

	public IMaterial[] Materials => materials;

	public void Dispose() {
		textureLoader.Dispose();
		foreach (var material in materials) {
			material.Dispose();
		}
	}
}
