using SharpDX.Direct3D11;
using System;
using System.Linq;

public class MaterialSet : IDisposable {
	public static MaterialSet LoadActive(Device device, ShaderCache shaderCache, TextureCache textureCache, IArchiveDirectory dataDir, IArchiveDirectory figureDir, string materialSetName, SurfaceProperties surfaceProperties) {
		var materialsDirectory = figureDir
			.Subdirectory("material-sets")
			.Subdirectory(materialSetName);

		var texturesDirectory = surfaceProperties.ShareTextures != null ?
			dataDir.Subdirectory("textures").Subdirectory(surfaceProperties.ShareTextures) :
			materialsDirectory;
		
		var textureLoader = new TextureLoader(device, textureCache, texturesDirectory);
		var multiMaterialSettings = Persistance.Load<MultiMaterialSettings>(materialsDirectory.File("material-settings.dat"));

		var materialSettingsBySurface = multiMaterialSettings.PerMaterialSettings;

		var rnd = new Random();
		foreach (var variantCategory in multiMaterialSettings.VariantCategories) {
			var variant = variantCategory.Variants[rnd.Next(variantCategory.Variants.Length)];
			for (int variantSurfaceIdx = 0; variantSurfaceIdx < variantCategory.Surfaces.Length; ++variantSurfaceIdx) {
				int surfaceIdx = variantCategory.Surfaces[variantSurfaceIdx];
				var materialSettings = variant.SettingsBySurface[variantSurfaceIdx];
				materialSettingsBySurface[surfaceIdx] = materialSettings;
			}
		}

		var materials = materialSettingsBySurface.Select(settings => settings.Load(device, shaderCache, textureLoader)).ToArray();

		float[] faceTransparencies = materialsDirectory.File("face-transparencies.array").ReadArray<float>();

		return new MaterialSet(textureLoader, materials, faceTransparencies);
	}
	
	private readonly TextureLoader textureLoader;
	private IMaterial[] materials;
	private float[] faceTransparencies;

	public MaterialSet(TextureLoader textureLoader, IMaterial[] materials, float[] faceTransparencies) {
		this.textureLoader = textureLoader;
		this.materials = materials;
		this.faceTransparencies = faceTransparencies;
	}

	public IMaterial[] Materials => materials;
	public float[] FaceTransparencies => faceTransparencies;

	public void Dispose() {
		textureLoader.Dispose();
		foreach (var material in materials) {
			material.Dispose();
		}
	}
}
