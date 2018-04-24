//returns a gloss as a premultiplied alpha layer
float4 calculateGenericGlossLayer(
	PixelInput input,
	float3 normal,
	float glossRoughness,
	float3 glossNormalColor,
	float3 glossGrazingColor,
	float glossNormalWeight,
	float glossGrazingWeight) {
	float3 glossIllumination = sampleGlossyIllumination(input, normal, glossRoughness);
	
	//float3 worldNormal = normalize(convertToObjectSpaceNormal(input, normal));
	//float grazingness = pow(1 - saturate(dot(input.eyeDirection, worldNormal)), 5) * (1 - glossRoughness);
	float grazingness = 0;

	float4 normalResult = glossNormalWeight == 0 ? float4(0, 0, 0, 0) : float4(glossNormalColor * glossIllumination, 1) * glossNormalWeight;
	float4 grazingResult = glossGrazingWeight == 0 ? float4(0, 0, 0, 0) : float4(glossGrazingColor * glossIllumination, 1) * glossGrazingWeight;

	return lerp(normalResult, grazingResult, grazingness);
}
