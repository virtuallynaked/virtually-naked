#include <texturing/StandardSamplers.hlsl>

Texture2D sourceTexture;

float4 main(float4 screenPosition : SV_POSITION, float2 texcoord : TEXCOORD) : SV_TARGET {
	return sourceTexture.Sample(trilinearSampler, texcoord);
}
