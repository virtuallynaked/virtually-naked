using System;
public class TextureImporter {
	public static TextureImporter Make(TextureProcessor textureProcessor, Figure figure, string uvSetName, int surfaceIdx) {
		var uvSet = figure.UvSets[uvSetName];
		var mask = TextureMask.Make(uvSet, figure.Geometry.SurfaceMap, surfaceIdx);
		return new TextureImporter(textureProcessor, mask);
	}

	private readonly TextureProcessor textureProcessor;
	private readonly TextureMask mask;
	
	public TextureImporter(TextureProcessor textureProcessor, TextureMask mask) {
		this.textureProcessor = textureProcessor;
		this.mask = mask;
	}
	
	public static bool IsLinear(RawImageInfo imageInfo, TextureProcessingType type) {
		if (imageInfo.gamma != 0 && imageInfo.gamma != 1 && imageInfo.gamma != 2.2f) {
			throw new InvalidOperationException("expected image gamma to be 0, 1 or 2.2");
		}

		bool isLinear = imageInfo.gamma == 1 || type == TextureProcessingType.Normal || type == TextureProcessingType.Bump;
		return isLinear;
	}

	private string ExtractImage(RawImageInfo imageInfo, TextureProcessingType type) {
		if (imageInfo == null) {
			return null;
		}
		bool isLinear = IsLinear(imageInfo, type);

		return textureProcessor.RegisterForProcessing(imageInfo.file, type, isLinear, mask);
	}

	public ColorTexture ImportColorTexture(RawColorTexture rawTexture) {
		ColorTexture texture = new ColorTexture {
			image = ExtractImage(rawTexture.image, TextureProcessingType.Color),
			value = rawTexture.value
		};
		return texture;
	}

	public FloatTexture ImportFloatTexture(RawFloatTexture rawTexture) {
		FloatTexture texture = new FloatTexture {
			image = ExtractImage(rawTexture.image, TextureProcessingType.SingleChannel),
			value = rawTexture.value
		};
		return texture;
	}

	public FloatTexture ImportNormalTexture(RawFloatTexture rawTexture) {
		FloatTexture texture = new FloatTexture() {
			image = ExtractImage(rawTexture.image, TextureProcessingType.Normal),
			value = rawTexture.value
		};

		if (texture.value != 1) {
			if (texture.value != 0) {
				Console.WriteLine("warning: normal map with non-unity multiplier");
			}
			texture.value = 1;
		}
		return texture;
	}

	public FloatTexture ImportBumpTexture(RawFloatTexture rawTexture) {
		FloatTexture texture = new FloatTexture() {
			image = ExtractImage(rawTexture.image, TextureProcessingType.Bump),
			value = rawTexture.value
		};
		return texture;
	}
}
