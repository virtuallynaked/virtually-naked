using System;

public class UberMaterialImporter : IMaterialImporter {
	private readonly Figure figure;
	private readonly TextureProcessor textureProcessor;
	private readonly FaceTransparencyProcessor faceTransparencyProcessor;

	public UberMaterialImporter(Figure figure, TextureProcessor textureProcessor, FaceTransparencyProcessor faceTransparencyProcessor) {
		this.figure = figure;
		this.textureProcessor = textureProcessor;
		this.faceTransparencyProcessor = faceTransparencyProcessor;
	}
	
	private RawUberMaterialSettings ImportRaw(MaterialBag bag) {
		RawUberMaterialSettings settings = new RawUberMaterialSettings { };
		
		settings.uvSet = bag.ExtractUvSetName(figure);

		settings.baseMixing = bag.ExtractInteger("extra/studio_material_channels/channels/Base Mixing");

		//Base / Diffuse / Reflection
		settings.metallicWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Metallic Weight");
		settings.diffuseWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Diffuse Weight");
		settings.baseColor = bag.ExtractColorTexture("diffuse");

		//Base / Diffuse / Translucency
		settings.translucencyWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Translucency Weight");
		settings.baseColorEffect = bag.ExtractInteger("extra/studio_material_channels/channels/Base Color Effect");
		settings.translucencyColor = bag.ExtractColorTexture("extra/studio_material_channels/channels/Translucency Color");
		settings.sssReflectanceTint = bag.ExtractColor("extra/studio_material_channels/channels/SSS Reflectance Tint");

		//Base / Glossy / Reflection
		settings.glossyWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Glossy Weight");
		settings.glossyLayeredWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Glossy Layered Weight");
		settings.glossyColor = bag.ExtractColorTexture("extra/studio_material_channels/channels/Glossy Color");
		settings.glossyColorEffect = bag.ExtractInteger("extra/studio_material_channels/channels/Glossy Color Effect");
		settings.glossyReflectivity = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Glossy Reflectivity");
		settings.glossySpecular = bag.ExtractColorTexture("extra/studio_material_channels/channels/Glossy Specular");
		settings.glossyRoughness = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Glossy Roughness");
		settings.glossiness = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Glossiness");

		//Base / Glossy / Refraction
		settings.refractionIndex = bag.ExtractFloat("extra/studio_material_channels/channels/Refraction Index");
		settings.refractionWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Refraction Weight");

		//Base / Bump
		settings.bumpStrength = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Bump Strength");
		settings.normalMap = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Normal Map");

		//Top Coat
		settings.topCoatWeight = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Weight");
		settings.topCoatColor = bag.ExtractColorTexture("extra/studio_material_channels/channels/Top Coat Color");
		settings.topCoatColorEffect = bag.ExtractInteger("extra/studio_material_channels/channels/Top Coat Color Effect");
		settings.topCoatRoughness = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Roughness");
		settings.topCoatGlossiness = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Glossiness");
		settings.topCoatLayeringMode = bag.ExtractInteger("extra/studio_material_channels/channels/Top Coat Layering Mode");
		settings.topCoatReflectivity = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Reflectivity");
		settings.topCoatIor = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat IOR");
		settings.topCoatCurveNormal = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Curve Normal");
		settings.topCoatCurveGrazing = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Curve Grazing");

		//Top Coat / Bump
		settings.topCoatBumpMode = bag.ExtractInteger("extra/studio_material_channels/channels/Top Coat Bump Mode");
		settings.topCoatBump = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Top Coat Bump");

		//Volume
		settings.thinWalled = bag.ExtractBoolean("extra/studio_material_channels/channels/Thin Walled");
		settings.transmittedMeasurementDistance = bag.ExtractFloat("extra/studio_material_channels/channels/Transmitted Measurement Distance");
		settings.transmittedColor = bag.ExtractColor("extra/studio_material_channels/channels/Transmitted Color");
		settings.scatteringMeasurementDistance = bag.ExtractFloat("extra/studio_material_channels/channels/Scattering Measurement Distance");
		settings.sssAmount = bag.ExtractFloat("extra/studio_material_channels/channels/SSS Amount");
		settings.sssDirection = bag.ExtractFloat("extra/studio_material_channels/channels/SSS Direction");

		//Geometry / Cutout
		settings.cutoutOpacity = bag.ExtractFloatTexture("extra/studio_material_channels/channels/Cutout Opacity");

		return settings;
	}

	public IMaterialSettings Import(int surfaceIdx, MaterialBag bag) {
		var rawSettings = ImportRaw(bag);

		UberMaterialSettings settings = new UberMaterialSettings { };
		
		settings.uvSet = rawSettings.uvSet;

		settings.baseMixing = rawSettings.baseMixing;

		var textureImporter = TextureImporter.Make(textureProcessor, figure, rawSettings.uvSet, surfaceIdx);

		//Base / Diffuse / Reflection
		settings.metallicWeight = textureImporter.ImportFloatTexture(rawSettings.metallicWeight);
		settings.diffuseWeight = textureImporter.ImportFloatTexture(rawSettings.diffuseWeight);
		settings.baseColor = textureImporter.ImportColorTexture(rawSettings.baseColor);

		//Base / Diffuse / Translucency
		settings.translucencyWeight = textureImporter.ImportFloatTexture(rawSettings.translucencyWeight);
		settings.baseColorEffect = rawSettings.baseColorEffect;
		settings.translucencyColor = textureImporter.ImportColorTexture(rawSettings.translucencyColor);
		settings.sssReflectanceTint = rawSettings.sssReflectanceTint;

		//Base / Glossy / Reflection
		settings.glossyWeight = textureImporter.ImportFloatTexture(rawSettings.glossyWeight);
		settings.glossyLayeredWeight = textureImporter.ImportFloatTexture(rawSettings.glossyLayeredWeight);
		settings.glossyColor = textureImporter.ImportColorTexture(rawSettings.glossyColor);
		settings.glossyColorEffect = rawSettings.glossyColorEffect;
		settings.glossyReflectivity = textureImporter.ImportFloatTexture(rawSettings.glossyReflectivity);
		settings.glossySpecular = textureImporter.ImportColorTexture(rawSettings.glossySpecular);
		settings.glossyRoughness = textureImporter.ImportFloatTexture(rawSettings.glossyRoughness);
		settings.glossiness = textureImporter.ImportFloatTexture(rawSettings.glossiness);

		//Base / Glossy / Refraction
		settings.refractionIndex = rawSettings.refractionIndex;
		settings.refractionWeight = textureImporter.ImportFloatTexture(rawSettings.refractionWeight);

		//Base / Bump
		settings.bumpStrength = textureImporter.ImportBumpTexture(rawSettings.bumpStrength);
		settings.normalMap = textureImporter.ImportNormalTexture(rawSettings.normalMap);

		//Top Coat
		settings.topCoatWeight = textureImporter.ImportFloatTexture(rawSettings.topCoatWeight);
		settings.topCoatColor = textureImporter.ImportColorTexture(rawSettings.topCoatColor);
		settings.topCoatColorEffect = rawSettings.topCoatColorEffect;
		settings.topCoatRoughness = textureImporter.ImportFloatTexture(rawSettings.topCoatRoughness);
		settings.topCoatGlossiness = textureImporter.ImportFloatTexture(rawSettings.topCoatGlossiness);
		settings.topCoatLayeringMode = rawSettings.topCoatLayeringMode;
		settings.topCoatReflectivity = textureImporter.ImportFloatTexture(rawSettings.topCoatReflectivity);
		settings.topCoatIor = textureImporter.ImportFloatTexture(rawSettings.topCoatIor);
		settings.topCoatCurveNormal = textureImporter.ImportFloatTexture(rawSettings.topCoatCurveNormal);
		settings.topCoatCurveGrazing = textureImporter.ImportFloatTexture(rawSettings.topCoatCurveGrazing);

		//Top Coat / Bump
		if (rawSettings.topCoatBumpMode != 0) {
			Console.WriteLine("warning: skipping top coat bump map with mode 'normal'");
			settings.topCoatBump = new FloatTexture { image= null, value = 1 };
		} else {
			settings.topCoatBump = textureImporter.ImportBumpTexture(rawSettings.topCoatBump);
		}

		//Volume
		settings.thinWalled = rawSettings.thinWalled;
		settings.transmittedMeasurementDistance = rawSettings.transmittedMeasurementDistance;
		settings.transmittedColor = rawSettings.transmittedColor;
		settings.scatteringMeasurementDistance = rawSettings.scatteringMeasurementDistance;
		settings.sssAmount = rawSettings.sssAmount;
		settings.sssDirection = rawSettings.sssDirection;

		//Geometry / Cutout
		settings.cutoutOpacity = textureImporter.ImportFloatTexture(rawSettings.cutoutOpacity);

		// process face transparencies
		if (settings.thinWalled && settings.refractionWeight.value > 0) {
			//HACK: assume refractive surfaces are fully transparent
			faceTransparencyProcessor.ProcessConstantSurface(surfaceIdx, 0);
		} else {
			faceTransparencyProcessor.ProcessSurface(surfaceIdx, rawSettings.uvSet, rawSettings.cutoutOpacity);
		}

		return settings;
	}
}
