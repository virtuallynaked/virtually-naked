#include <d3d/VertexCommon.hlsl>

struct VertexIn {
	float3 position : POSITION;
	float3 normal : NORMAL;
};

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

VertexOutput main(VertexIn vIn) {
	VertexOutput vOut = (VertexOutput) 0;
	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;
	vOut.positions = calculatePositions(worldPosition);
	vOut.normal = normalize(mul(vIn.normal, worldMatrixInverseTranspose));
	vOut.texcoord = float2(0.5, 0.5);
	return vOut;
}
