#include <viewer/VertexCommon.hlsl>

struct VertexIn {
	float3 position : POSITION;
	float2 texcoord : TEXCOORD;
	float3 normal : NORMAL;
};

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

VertexOutput main(VertexIn vIn) {
	VertexOutput vOut;

	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;
	vOut.positions = calculatePositions(worldPosition);

	vOut.normal = normalize(mul(vIn.normal, worldMatrixInverseTranspose));
	vOut.tangent = 0;
	
	vOut.texcoord = vIn.texcoord;

	vOut.occlusion = 0;
	vOut.scatteredIllumination = 0;

	return vOut;
}