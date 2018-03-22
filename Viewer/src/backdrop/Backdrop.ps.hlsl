#include <texturing/ImageBasedLightingCommon.hlsl>
#include <texturing/StandardSamplers.hlsl>
#include "BackdropVsToPs.hlsl"

float4 main(BackdropVsToPs vsToPs) : SV_Target {
	float3 observerPosition = float3(0, 1.75, 0);
	float3 direction = vsToPs.worldPosition - observerPosition;
	float3 color = glossyEnvironmentCube.SampleLevel(trilinearSampler, float4(mul(direction, environmentMirror), 0), 0).rgb;
	return float4(color, 1);
}
