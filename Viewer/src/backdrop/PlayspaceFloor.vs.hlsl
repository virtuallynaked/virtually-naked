#include <viewer/VertexCommon.hlsl>

cbuffer ConstantBuffer1 : register(b1) {
	float3 playspaceBounds[4];
}

float4 main(uint vI : SV_VERTEXID) : SV_Position {
	float3 worldPosition = playspaceBounds[vI];
	float4 screenPosition = calculateScreenPosition(worldPosition);
	return screenPosition;
}