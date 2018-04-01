#include <d3d/VertexCommon.hlsl>
#include <game/rendering/RenderingConstants.hlsl>
#include "RefinedVertex.hlsl"

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

static const float DepthBias = -0.02;

float4 applyDepthBias(float4 screenPosition, float bias, float near, float far) {
	float z = -screenPosition.w;
	float zProjBiased = (far*(bias + near + z)) / ((far - near)*(bias + z));
	float zScreenBiased = zProjBiased * screenPosition.w;

	return float4(screenPosition.x, screenPosition.y, zScreenBiased, screenPosition.w);
}

float4 main(RefinedVertex vIn) : SV_POSITION{
	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;
	float4 screenPosition = calculateScreenPosition(worldPosition);
	float4 biasedScreenPosition = applyDepthBias(screenPosition, DepthBias, znear, zfar);
	return biasedScreenPosition;
}
