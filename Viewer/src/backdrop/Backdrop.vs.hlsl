#include <viewer/VertexCommon.hlsl>
#include "BackdropVsToPs.hlsl"

struct VertexIn {
	float3 position : POSITION;
	float3 normal : NORMAL;
};

cbuffer ConstantBuffer1 : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

BackdropVsToPs main(VertexIn vIn) {
	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;

	BackdropVsToPs vsToPs;
	vsToPs.worldPosition = worldPosition;
	vsToPs.screenPosition = calculateScreenPosition(worldPosition);
	return vsToPs;
}