#include <texturing/ImageBasedLightingCommon.hlsl>
#include <texturing/StandardSamplers.hlsl>
#include "UberUtilities.hlsl"
#include "UberInputs.hlsl"
#include "UberGenericGloss.hlsl"
#include "UberTopCoat.hlsl"
#include "UberBase.hlsl"
#include "UberUnified.hlsl"

[earlydepthstencil]
float4 main(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) : SV_Target {
	PixelInput input = preparePixelInput(vertexOutput, isFrontFace);
	return calculateUnifiedResult(input);
}
