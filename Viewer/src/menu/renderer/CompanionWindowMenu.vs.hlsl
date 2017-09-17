#include <viewer/companion-window/AspectRatiosConstantBuffer.hlsl>
#include "MenuPsToVs.hlsl"

MenuPsToVs main(uint vI : SV_VERTEXID) {
	float2 zeroOneCoord = float2(vI & 1, vI >> 1);

	MenuPsToVs output;
	output.texcoord = zeroOneCoord;

	float2 objectPosition = float2(zeroOneCoord.x * 2 - 1, -(zeroOneCoord.y * 2 - 1));

	float2 aspectRatioAdjustment;
	if (companionWindowAspectRatio < 1) {
		aspectRatioAdjustment = float2(1, companionWindowAspectRatio);
	} else {
		aspectRatioAdjustment = float2(1 / companionWindowAspectRatio, 1);
	}

	output.screenPosition = float4(
		aspectRatioAdjustment * objectPosition * 0.5 + float2(0, -0.5),
		0,
		1);

	return output;
}