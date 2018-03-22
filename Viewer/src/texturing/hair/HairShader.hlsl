#include <texturing/ImageBasedLightingCommon.hlsl>
#include <texturing/StandardSamplers.hlsl>

Texture2D DiffuseTex : register(t2);
Texture2D OpacityTex : register(t3);

struct Result {
	float3 color;
	float opacity;
	float visibility;
};

Result calculateResult(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) {
	float opacity = OpacityTex.Sample(anisotropicSampler, vertexOutput.texcoord).r;
	if (opacity < 1e-3) {
		discard;
	}

	PixelInput input = preparePixelInput(vertexOutput, isFrontFace);

	float3 illumination = sampleDiffuseIllumination(input, GeometryNormal);
	float3 albedo = DiffuseTex.Sample(anisotropicSampler, input.texcoord).rgb;
	float3 diffuseResult = illumination * albedo;

	Result result;
	result.color = diffuseResult;
	result.opacity = opacity;
	result.visibility = 1 - input.occlusion.x;
	return result;
}
