#include "AspectRatiosConstantBuffer.hlsl"

struct TexcoordOnlyPixelInput {
	float4 screenPosition : SV_POSITION;
	float2 texcoord : TEXCOORD;
};

static const float PovReductionScale = 1 / 1.5f;

float2 calculateScale() {
	//compensate for aspect ratio
	float2 scale = float2(
		companionWindowAspectRatio,
		sourceAspectRatio);

	//rescale standard size
	float maxScale = max(scale.x, scale.y);
	scale /= maxScale;

	//rescale to hide hidden area and reduce POV
	scale *= PovReductionScale;

	return scale;
}

TexcoordOnlyPixelInput main(uint vI : SV_VERTEXID) {
	float2 zeroOneCoord = float2(vI & 1, vI >> 1);

	float2 scale = calculateScale();

	TexcoordOnlyPixelInput output;
	output.texcoord = (zeroOneCoord - 0.5) * scale + 0.5;
	output.screenPosition = float4(zeroOneCoord.x * 2 - 1, -(zeroOneCoord.y * 2 - 1), 0, 1);
	return output;
}
