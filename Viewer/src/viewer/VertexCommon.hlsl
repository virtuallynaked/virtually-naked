#include <viewer/VertexOutput.hlsl>

cbuffer ViewProjectionBuffer : register(b0) {
	float4x4 viewProjectionMatrix;
	float3 eyePosition;
}

float4 calculateScreenPosition(float3 worldPosition) {
	return mul(float4(worldPosition, 1), viewProjectionMatrix);
}

VertexOutputPositions calculatePositions(float3 worldPosition) {
	VertexOutputPositions positions;
	positions.objectRelativeEyePosition = eyePosition - worldPosition;
	positions.screenPosition = calculateScreenPosition(worldPosition);
	return positions;
}
