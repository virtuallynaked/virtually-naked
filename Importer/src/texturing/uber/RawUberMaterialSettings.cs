using SharpDX;

public class RawUberMaterialSettings {
	public string uvSet;

	public int baseMixing;

	//Base / Diffuse / Reflection
	public RawFloatTexture metallicWeight;
	public RawFloatTexture diffuseWeight;
	public RawColorTexture baseColor;

	//Base / Diffuse / Translucency
	public RawFloatTexture translucencyWeight;
	public int baseColorEffect;
	public RawColorTexture translucencyColor;
	public Vector3 sssReflectanceTint;

	//Base / Glossy / Reflection
	public RawFloatTexture glossyWeight;
	public RawFloatTexture glossyLayeredWeight;
	public RawColorTexture glossyColor;
	public int glossyColorEffect;
	public RawFloatTexture glossyReflectivity;
	public RawColorTexture glossySpecular;
	public RawFloatTexture glossyRoughness;
	public RawFloatTexture glossiness;

	//Base / Glossy / Refraction
	public float refractionIndex;
	public RawFloatTexture refractionWeight;

	//Base / Bump
	public RawFloatTexture bumpStrength;
	public RawFloatTexture normalMap;

	//Top Coat / General
	public RawFloatTexture topCoatWeight;
	public RawColorTexture topCoatColor;
	public int topCoatColorEffect;
	public RawFloatTexture topCoatRoughness;
	public RawFloatTexture topCoatGlossiness;
	public int topCoatLayeringMode;
	public RawFloatTexture topCoatReflectivity;
	public RawFloatTexture topCoatIor;
	public RawFloatTexture topCoatCurveNormal;
	public RawFloatTexture topCoatCurveGrazing;

	//Top Coat / Bump
	public int topCoatBumpMode;
	public RawFloatTexture topCoatBump;

	//Volume
	public bool thinWalled;
	public float transmittedMeasurementDistance;
	public Vector3 transmittedColor;
	public float scatteringMeasurementDistance;
	public float sssAmount;
	public float sssDirection;

	//Geometry / Cutout
	public RawFloatTexture cutoutOpacity;
}


