#include <d3d/VertexCommon.hlsl>
#include "RefinedVertex.hlsl"

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

static const float DepthBias = 0.5; //in centimeters

float4 main(RefinedVertex vIn) : SV_POSITION {
	float3 position = vIn.position;
	float3 worldPosition = mul(float4(position, 1), worldMatrix).xyz;
	float4 screenPosition = calculateScreenPosition(worldPosition);

	float3 erodedPosition = vIn.position - DepthBias * vIn.normal;
	float3 erodedWorldPosition = mul(float4(erodedPosition, 1), worldMatrix).xyz;
	float4 erodedScreenPosition = calculateScreenPosition(erodedWorldPosition);
	
	float4 combinedScreenPosition = float4(
		screenPosition.x,
		screenPosition.y,
		min(screenPosition.z, erodedScreenPosition.z / erodedScreenPosition.w * screenPosition.w),
		screenPosition.w);
	return combinedScreenPosition;
}
