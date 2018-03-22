using SharpDX;
using System;

public class HairMaterialImporter : IMaterialImporter {
	private readonly Figure figure;
	private readonly TextureProcessor textureProcessor;
	
	public HairMaterialImporter(Figure figure, TextureProcessor textureProcessor) {
		this.figure = figure;
		this.textureProcessor = textureProcessor;
	}

	public IMaterialSettings Import(int surfaceIdx, MaterialBag bag) {
		string uvSet = bag.ExtractUvSetName(figure);

		var textureImporter = TextureImporter.Make(textureProcessor, figure, uvSet, surfaceIdx);
		
		var diffuseTexture = textureImporter.ImportColorTexture(bag.ExtractColorTexture("diffuse"));
		
		FloatTexture opacityTexture;
		if (bag.HasExtraType(MaterialBag.DazBrickType) || bag.HasExtraType(MaterialBag.IrayUberType)) {
			opacityTexture = textureImporter.ImportFloatTexture(bag.ExtractFloatTexture("extra/studio_material_channels/channels/Cutout Opacity"));
		} else {
			opacityTexture = new FloatTexture {
				value = 1,
				image = null
			};
		}
		
		return new HairMaterialSettings {
			uvSet = uvSet,
			diffuseAlbedo = diffuseTexture,
			opacity = opacityTexture
		};
	}
}
