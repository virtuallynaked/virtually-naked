#include <texturing/StandardSamplers.hlsl>

cbuffer buffer0 : register(b0) {
	float3 exposureAdjustment;
	float burnHighlightsParameter;
	float crushBlacksParameter;
};

Texture2D<float3> sourceTexture;

static const float3 LuminanceFactors = float3(0.17697, 0.8124, 0.01063);

float3 burnHighlights(float3 c, float a) {
	return c * (1 + a * c) / (1 + c);
}

float3 crushBlacks(float3 c, float a) {
	float lum = dot(LuminanceFactors, c);
	float sqrtLum = sqrt(lum);
	float3 s = pow(saturate(c), 2 * a);
	float3 factors = lerp(sqrtLum, 1, s);
	return c * factors;
}

float4 main(float4 screenPosition : SV_POSITION) : SV_TARGET {
	float3 color = (float3) sourceTexture.Load(int3(screenPosition.xy, 0));
	color *= exposureAdjustment;
	color = burnHighlights(color, burnHighlightsParameter);
	color = crushBlacks(color, crushBlacksParameter);

	return float4(color, 1);
}
