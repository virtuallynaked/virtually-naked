#include <texturing/ImageBasedLightingCommon.hlsl>
#include <texturing/StandardSamplers.hlsl>

Texture2D albedoTexture : register(t2);

cbuffer cbuffer0 : register(b0) {
	float roughness;
	float specular;
};

float4 main(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) : SV_Target {
	PixelInput input = preparePixelInput(vertexOutput, isFrontFace);

	float3 diffuseIllumination = sampleDiffuseIllumination(input, GeometryNormal);
	float3 glossyIllumination = sampleGlossyIllumination(input, GeometryNormal, roughness);
	
	float3 albedo = albedoTexture.Sample(anisotropicSampler, input.texcoord).rgb;
	float3 color = lerp(diffuseIllumination * albedo, glossyIllumination, specular);
	
	return float4(color, 1);
}