//Base / Diffuse / Reflection
Texture2D MetallicWeightTex : register(t2);
Texture2D DiffuseWeightTex : register(t3);
Texture2D BaseColorTex : register(t4);

//Base / Diffuse Translucency
Texture2D TranslucencyWeightTex : register(t5);
Texture2D TranslucencyColorTex : register(t6);

//Base / Glossy / Reflection
Texture2D GlossyWeightTex : register(t7);
Texture2D GlossyLayeredWeightTex : register(t8);
Texture2D GlossyColorTex : register(t9);
Texture2D GlossySpecularTex : register(t10);
Texture2D GlossinessTex : register(t11);
Texture2D GlossyReflectivityTex : register(t12);
Texture2D GlossyRoughnessTex : register(t13);

//Base / Glossy / Refraction
Texture2D RefractionWeightTex : register(t14);

//Base / Bump
Texture2D BumpStrengthTex : register(t15);
Texture2D NormalMapTex : register(t16);

// Top Coat
Texture2D TopCoatWeightTex : register(t17);
Texture2D TopCoatColorTex : register(t18);
Texture2D TopCoatRoughnessTex : register(t19);
Texture2D TopCoatReflectivityTex : register(t20);
Texture2D TopCoatIORTex : register(t21);
Texture2D TopCoatCurveNormalTex : register(t22);
Texture2D TopCoatCurveGrazingTex : register(t23);

// Top Coat / Bump
Texture2D TopCoatBumpTex : register(t24);

//Geometry/Cutout
Texture2D CutoutOpacityTex : register(t25);

static const int BaseMixingMode_PBR_MetallicityRoughness = 0;
static const int BaseMixingMode_PBR_SpecularGlossiness = 1;
static const int BaseMixingMode_Weighted = 2;

static const int TopCoatLayeringMode_Reflectivity = 0;
static const int TopCoatLayeringMode_Weighted = 1;
static const int TopCoatLayeringMode_Fresnel = 2;
static const int TopCoatLayeringMode_CustomCurve = 3;

cbuffer cbuffer0 : register(b0) {
	//Base / Mixing
	int BaseMixingMode;

	//Base / Diffuse / Reflection
	float MetallicWeight;
	float DiffuseWeight;
	float3 BaseColor;
	
	//Base / Diffuse Translucency
	float TranslucencyWeight;
	int BaseColorEffect;
	float3 TranslucencyColor;
	float3 SSSReflectanceTint;

	//Base / Glossy / Reflection
	float GlossyWeight;
	float GlossyLayeredWeight;
	float3 GlossyColor;
	int GlossyColorEffect;
	float3 GlossySpecular;
	float Glossiness;
	float GlossyReflectivity;
	float GlossyRoughness;

	//Base / Glossy / Refraction
	float RefractionIndex;
	float RefractionWeight;

	//Base / Bump
	float BumpStrength;
	float NormalMap;

	// Top Coat
	float TopCoatWeight;
	float3 TopCoatColor;
	int TopCoatColorEffect;
	float TopCoatRoughness;
	int TopCoatLayeringMode;
	float TopCoatReflectivity;
	float TopCoatIOR;
	float TopCoatCurveNormal;
	float TopCoatCurveGrazing;

	// Top Coat Bump
	float TopCoatBump;

	// Volume
	int ThinWalled;
	float3 VolumeColor;

	// Geometry/Cutout
	float CutoutOpacity;
};

