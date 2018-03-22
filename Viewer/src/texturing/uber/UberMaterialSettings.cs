using ProtoBuf;
using SharpDX;
using System;
using SharpDX.Direct3D11;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ColorTexture {
	public Vector3 value;
	public string image;
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FloatTexture {
	public float value;
	public string image;
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class UberMaterialSettings : IMaterialSettings {
	public string uvSet;

	public int baseMixing;

	//Base / Diffuse / Reflection
	public FloatTexture metallicWeight;
	public FloatTexture diffuseWeight;
	public ColorTexture baseColor;

	//Base / Diffuse / Translucency
	public FloatTexture translucencyWeight;
	public int baseColorEffect;
	public ColorTexture translucencyColor;
	public Vector3 sssReflectanceTint;

	//Base / Glossy / Reflection
	public FloatTexture glossyWeight;
	public FloatTexture glossyLayeredWeight;
	public ColorTexture glossyColor;
	public int glossyColorEffect;
	public FloatTexture glossyReflectivity;
	public ColorTexture glossySpecular;
	public FloatTexture glossyRoughness;
	public FloatTexture glossiness;

	//Base / Glossy / Refraction
	public float refractionIndex;
	public FloatTexture refractionWeight;

	//Base / Bump
	public FloatTexture bumpStrength;
	public FloatTexture normalMap;

	//Top Coat / General
	public FloatTexture topCoatWeight;
	public ColorTexture topCoatColor;
	public int topCoatColorEffect;
	public FloatTexture topCoatRoughness;
	public FloatTexture topCoatGlossiness;
	public int topCoatLayeringMode;
	public FloatTexture topCoatReflectivity;
	public FloatTexture topCoatIor;
	public FloatTexture topCoatCurveNormal;
	public FloatTexture topCoatCurveGrazing;

	//Top Coat / Bump
	public FloatTexture topCoatBump;

	//Volume
	public bool thinWalled;
	public float transmittedMeasurementDistance;
	public Vector3 transmittedColor;
	public float scatteringMeasurementDistance;
	public float sssAmount;
	public float sssDirection;

	//Geometry / Cutout
	public FloatTexture cutoutOpacity;

	public IMaterial Load(Device device, ShaderCache shaderCache, TextureLoader textureLoader) {
		return UberMaterial.Load(device, shaderCache, textureLoader, this);
	}
}
