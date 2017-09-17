float3 addGenericGloss(
	PixelInput input,
	float3 normal,
	float3 baseResult,
	float glossRoughness,
	float3 glossNormalColor,
	float3 glossGrazingColor,
	float glossNormalWeight,
	float glossGrazingWeight) {
	float3 glossIllumination = sampleGlossyIllumination(input, normal, glossRoughness);
	
	//float3 worldNormal = normalize(convertToObjectSpaceNormal(input, normal));
	//float grazingness = pow(1 - saturate(dot(input.eyeDirection, worldNormal)), 5) * (1 - glossRoughness);
	float grazingness = 0;
	
	float3 normalResult = glossNormalWeight == 0 ? baseResult : lerp(baseResult, glossNormalColor * glossIllumination, glossNormalWeight);
	float3 grazingResult = glossGrazingWeight == 0 ? baseResult : lerp(baseResult, glossGrazingColor * glossIllumination, glossGrazingWeight);

	return lerp(normalResult, grazingResult, grazingness);
}
