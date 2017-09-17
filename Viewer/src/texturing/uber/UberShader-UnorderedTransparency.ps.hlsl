#include <texturing/ImageBasedLightingCommon.hlsl>
#include <texturing/StandardSamplers.hlsl>
#include "UberUtilities.hlsl"
#include "UberInputs.hlsl"
#include "UberGenericGloss.hlsl"
#include "UberTopCoat.hlsl"
#include "UberBase.hlsl"
#include "UberUnified.hlsl"

struct PSOut {
	float4 accumTarget : SV_Target0;
	float4 revealageTarget : SV_Target1;
};

PSOut main(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) {
	PixelInput input = preparePixelInput(vertexOutput, isFrontFace);
	float4 result = calculateUnifiedResult(input);

	float occlusion = input.occlusion.x;
	float revealage = 1 - occlusion;
	float weight = revealage * revealage;

	PSOut psOut;
	psOut.accumTarget = weight * result;
	psOut.revealageTarget = float4(0, 0, 0, result.a);
	return psOut;
}