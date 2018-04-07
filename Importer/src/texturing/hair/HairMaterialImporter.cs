using SharpDX;
using System;

public class HairMaterialImporter : IMaterialImporter {
	private readonly Figure figure;
	private readonly TextureProcessor textureProcessor;
	private readonly FaceTransparencyProcessor faceTransparencyProcessor;
	
	public HairMaterialImporter(Figure figure, TextureProcessor textureProcessor, FaceTransparencyProcessor faceTransparencyProcessor) {
		this.figure = figure;
		this.textureProcessor = textureProcessor;
		this.faceTransparencyProcessor = faceTransparencyProcessor;
	}

	public IMaterialSettings Import(int surfaceIdx, MaterialBag bag) {
		string uvSet = bag.ExtractUvSetName(figure);

		var textureImporter = TextureImporter.Make(textureProcessor, figure, uvSet, surfaceIdx);
		
		var diffuseTexture = textureImporter.ImportColorTexture(bag.ExtractColorTexture("diffuse"));
		
		FloatTexture opacityTexture;
		if (bag.HasExtraType(MaterialBag.DazBrickType) || bag.HasExtraType(MaterialBag.IrayUberType)) {
			var rawOpacityTexture = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Cutout Opacity");
			opacityTexture = textureImporter.ImportFloatTexture(rawOpacityTexture);
			faceTransparencyProcessor.ProcessSurface(surfaceIdx, uvSet, rawOpacityTexture);
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
