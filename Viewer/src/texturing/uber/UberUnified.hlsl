float4 applyRefraction(PixelInput input, float4 baseResult) {
	//TODO: use glossy color (currently assumed to be white)

	float glossRoughness;
	if (BaseMixingMode == BaseMixingMode_PBR_MetallicityRoughness) {
		glossRoughness = SAMPLE_FLOAT_TEX(GlossyRoughness);
	}
	else if (BaseMixingMode == BaseMixingMode_PBR_SpecularGlossiness) {
		glossRoughness = 1 - SAMPLE_FLOAT_TEX(Glossiness);
	}
	else { //BaseMixingMode == BaseMixingMode_Weighted
		glossRoughness = SAMPLE_FLOAT_TEX(GlossyRoughness);
	}

	float3 baseNormal = combineNormals(
		SAMPLE_NORMAL_TEX(NormalMap),
		SAMPLE_BUMP_TEX(BumpStrength));
	float3 baseNormalBroad = GeometryNormal;

	float3 glossIllumination = sampleGlossyIllumination(input, baseNormal, glossRoughness);

	float reflectivity = reflectivityFromIOR(RefractionIndex);

	float4 refractionResult;
	if (ThinWalled) {
		refractionResult = float4(glossIllumination, 1) * reflectivity;
	}
	else {
		float3 translucencyIllumination = input.scatteredIllumination;
		refractionResult = float4(glossIllumination * reflectivity + VolumeColor * translucencyIllumination * (1 - reflectivity), 1);
	}

	float refractionWeight = SAMPLE_FLOAT_TEX(RefractionWeight);
	float4 result = (1 - refractionWeight) * baseResult + refractionWeight * refractionResult;

	return result;
}

float4 calculateUnifiedResult(PixelInput input) {
	float opacity = saturate(SAMPLE_FLOAT_TEX(CutoutOpacity));
	if (opacity <= 0) {
		discard;
	}

	float3 result = calculateBaseResult(input);

	float4 premultipliedAlphaResult = float4(result, 1);
	premultipliedAlphaResult = applyRefraction(input, premultipliedAlphaResult);

	float4 topCoatLayer = calculateTopCoatLayer(input);
	premultipliedAlphaResult = applyLayer(premultipliedAlphaResult, topCoatLayer);

	premultipliedAlphaResult *= opacity;

	return premultipliedAlphaResult;
}
