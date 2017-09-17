#include <viewer/VertexCommon.hlsl>
#include "MenuPsToVs.hlsl"

static const float PI = 3.14159265f;

cbuffer cbuffer1 : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

MenuPsToVs main(uint vI : SV_VERTEXID) {
	float2 zeroOneCoord = float2(vI % 2, (float) (vI / 2) / 20);

	MenuPsToVs output;
	output.texcoord = zeroOneCoord;

	float t = zeroOneCoord.y;
	float radius = 2 / PI;
	float4 objectPosition = float4(zeroOneCoord.x * 2 - 1, -cos(zeroOneCoord.y * PI) * radius, -sin(zeroOneCoord.y * PI) * radius, 1);
	float3 worldPosition = mul(objectPosition, worldMatrix).xyz;
	output.screenPosition = calculateScreenPosition(worldPosition);
	return output;
}