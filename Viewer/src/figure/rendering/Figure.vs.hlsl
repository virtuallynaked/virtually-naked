#include <d3d/VertexCommon.hlsl>
#include "RefinedVertex.hlsl"

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

VertexOutput main(RefinedVertex vIn) {
	VertexOutput vOut;

	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;
	vOut.positions = calculatePositions(worldPosition);

	vOut.normal = mul(vIn.normal, worldMatrixInverseTranspose);
	
	vOut.tangent = mul(vIn.tangent, worldMatrixInverseTranspose);
	vOut.texcoord = float2(vIn.texCoord.x % 1, 1 - vIn.texCoord.y);

	vOut.secondaryTangent = mul(vIn.secondaryTangent, worldMatrixInverseTranspose);
	vOut.secondaryTexcoord = float2(vIn.secondaryTexCoord.x % 1, 1 - vIn.secondaryTexCoord.y);

	vOut.occlusion = vIn.occlusion;
	vOut.scatteredIllumination = vIn.scatteredIllumination;

	return vOut;
}
