#include <texturing/StandardSamplers.hlsl>
#include "MenuPsToVs.hlsl"

Texture2D sourceTexture : register(t2);

float4 main(MenuPsToVs input) : SV_TARGET {
	return sourceTexture.Sample(uiSampler, input.texcoord);
}
