#include <texturing/PixelInput.hlsl>
#include <texturing/StandardSamplers.hlsl>

TextureCube diffuseEnvironmentCube : register(t0);
TextureCubeArray glossyEnvironmentCube : register(t1);

static const float environmentIntensity = 4.0 / 3.0;
static const float RoughnessLevels = 10;

cbuffer cbuffer10 : register(b10) {
	float3x3 environmentMirror;
}

float3 sampleDiffuseIllumination(float3 normal, float occlusionAmount) {
	float3 illumination = diffuseEnvironmentCube.SampleLevel(trilinearSampler, mul(normal, environmentMirror), 0).rgb * (1 - occlusionAmount);
	return environmentIntensity * illumination;
}

float3 sampleDiffuseIllumination(PixelInput input, float3 tangentSpaceNormal) {
	float3 normal = normalize(convertToObjectSpaceNormal(input, tangentSpaceNormal));

	float occlusionAmount;
	if (tangentSpaceNormal.z >= 0) {
		occlusionAmount = input.occlusion.x;
	} else {
		occlusionAmount = input.occlusion.y;
	}

	return sampleDiffuseIllumination(normal, occlusionAmount);
}

float intensityAdjustment(float roughness) {
	//phongExponent = 2/(4*roughness^4)
	//blinnExponent = 2/(roughness^4)
	//phongFactor = (phongExponent + 1)/2
	//blinnFactor = (blinnExponent + 1)/8
	//adjustment = blinnFactor / phongFactor
	float roughnessPow2 = roughness * roughness;
	float roughnessPow4 = roughnessPow2 * roughnessPow2;
	return (2 + roughnessPow4) / (2 + 4 * roughnessPow4);
}

float3 sampleGlossyIllumination(PixelInput input, float3 tangentSpaceNormal, float roughness) {
	float3 normal = convertToObjectSpaceNormal(input, tangentSpaceNormal);
	float3 eyeDirection = input.eyeDirection;
	float3 reflectionDirection = reflect(-eyeDirection, normal);

	float w = roughness * RoughnessLevels;
	float wFloor = floor(w);
	float wCeil = ceil(w);
	float wFrac = w - wFloor;

	float3 illuminationFloor = glossyEnvironmentCube.Sample(trilinearSampler, float4(mul(reflectionDirection, environmentMirror), wFloor)).rgb;
	float3 illuminationCeil = glossyEnvironmentCube.Sample(trilinearSampler, float4(mul(reflectionDirection, environmentMirror), wCeil)).rgb;
	float3 illumination = lerp(illuminationFloor, illuminationCeil, wFrac);
	
	//apply occlusion
	illumination *= (1 - input.occlusion.x);

	//adjust to Daz Studio intensity
	illumination *= intensityAdjustment(roughness);

	return environmentIntensity * illumination;
}
