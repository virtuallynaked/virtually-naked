void transmitColorCalc(inout float3 color, inout float weight) {
	float maxVal = max(max(color.r, color.g), color.b);
	float meanVal = (color.r + color.g + color.b) / 3;

	color /= maxVal;
	weight *= meanVal;
}

float3 calculateDiffuseAndTranslucency(PixelInput input, float3 baseNormal) {
	float3 illumination = sampleDiffuseIllumination(input, baseNormal);
	
	float translucencyWeight = SAMPLE_FLOAT_TEX(TranslucencyWeight);
	float diffuseWeight = 1 - translucencyWeight;

	float3 diffuseColor = SAMPLE_COLOR_TEX(BaseColor);
	float3 translucencyColor = SAMPLE_COLOR_TEX(TranslucencyColor);
	
	if (BaseColorEffect == 1) {
		diffuseColor *= SSSReflectanceTint;
		transmitColorCalc(diffuseColor, diffuseWeight);
	}
	
	float3 reflectedAmount = diffuseColor * diffuseWeight;
	float3 transmittedAmount = translucencyColor * (1 - diffuseWeight);

	if (ThinWalled) {
		float3 reverseIllumination = sampleDiffuseIllumination(input, -baseNormal);
		return illumination * reflectedAmount + reverseIllumination * transmittedAmount;
	} else {
		float3 illuminationBroad = input.scatteredIllumination;

		float3 reflectedAmountBroad = reflectedAmount;
		float3 transmittedAmountBroad = transmittedAmount;

		float3 volumeColor = VolumeColor;

		return (illumination * reflectedAmount) + (illuminationBroad * transmittedAmount * transmittedAmountBroad * volumeColor) / (1 - reflectedAmountBroad * volumeColor);
	}
}

float3 calculateBaseResult(PixelInput input) {
	float3 baseNormal = combineNormals(
		SAMPLE_NORMAL_TEX(NormalMap),
		SAMPLE_BUMP_TEX(BumpStrength));

	float3 diffuse_bsdf_result = calculateDiffuseAndTranslucency(input, baseNormal);

	float3 glossyColor = SAMPLE_COLOR_TEX(GlossyColor);

	float glossyLayeredWeight;
	if (BaseMixingMode != BaseMixingMode_Weighted) {
		glossyLayeredWeight = SAMPLE_FLOAT_TEX(GlossyLayeredWeight);

		if (GlossyColorEffect == 1) {
			float mean = meanValue(glossyColor);
			float max = maxValue(glossyColor);
			glossyLayeredWeight *= mean;
			glossyColor /= max;
		}
	}
	else {
		glossyLayeredWeight = 0;
	}

	float glossRoughness;
	float3 glossNormalColor, glossGrazingColor;
	float glossNormalWeight, glossGrazingWeight;

	if (BaseMixingMode == BaseMixingMode_PBR_MetallicityRoughness) {
		glossRoughness = SAMPLE_FLOAT_TEX(GlossyRoughness);
		glossNormalColor = glossyColor;
		glossGrazingColor = glossyColor;
		glossNormalWeight = glossyLayeredWeight * SAMPLE_FLOAT_TEX(GlossyReflectivity) * 0.08;
		glossGrazingWeight = glossyLayeredWeight;
	}
	else if (BaseMixingMode == BaseMixingMode_PBR_SpecularGlossiness) {
		float3 glossySpecular = SAMPLE_COLOR_TEX(GlossySpecular);

		float normalWeight = 1;
		{
			float max = maxValue(glossySpecular);
			glossySpecular /= max;
			normalWeight *= max;
		}

		glossRoughness = 1 - SAMPLE_FLOAT_TEX(Glossiness);
		glossNormalColor = glossyColor * glossySpecular;
		glossGrazingColor = glossyColor;
		glossNormalWeight = glossyLayeredWeight * normalWeight;
		glossGrazingWeight = glossyLayeredWeight;
	}
	else { //BaseMixingMode == BaseMixingMode_Weighted
		float diffuseWeight = SAMPLE_FLOAT_TEX(DiffuseWeight);
		float glossyWeight = SAMPLE_FLOAT_TEX(GlossyWeight);
		float normalizedGlossWeight = glossyWeight / (glossyWeight + diffuseWeight);
		
		glossRoughness = SAMPLE_FLOAT_TEX(GlossyRoughness);
		glossNormalColor = glossyColor;
		glossGrazingColor = glossyColor;
		glossNormalWeight = normalizedGlossWeight;
		glossGrazingWeight = normalizedGlossWeight;
	}

	float3 result = addGenericGloss(
		input, baseNormal,
		diffuse_bsdf_result,
		glossRoughness,
		glossNormalColor, glossGrazingColor,
		glossNormalWeight, glossGrazingWeight
	);

	if (BaseMixingMode == BaseMixingMode_PBR_MetallicityRoughness) {
		float metallicWeight = SAMPLE_FLOAT_TEX(MetallicWeight);
		float3 baseColor = SAMPLE_COLOR_TEX(BaseColor);
		result = addGenericGloss(
			input, baseNormal,
			result,
			glossRoughness,
			baseColor, baseColor,
			metallicWeight, metallicWeight);
	}

	return result;
}
