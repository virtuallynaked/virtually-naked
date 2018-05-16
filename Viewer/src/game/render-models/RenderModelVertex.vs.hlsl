#include <d3d/VertexCommon.hlsl>

struct VertexIn {
	float3 position : POSITION;
	float2 texcoord : TEXCOORD;
	float3 normal : NORMAL;
};

cbuffer cbuffer1 : register(b1) {
	float4x4 objectCoordinateToWorldTransform;
	float3x3 objectNormalToWorldTransform;
}

cbuffer cbuffer2 : register(b2) {
	float4x4 componentCoordinateToObjectTransform;
	float3x3 componentNormalToObjectTransform;
}

VertexOutput main(VertexIn vIn) {
	VertexOutput vOut;

	float4 componentSpacePosition = float4(vIn.position, 1);
	float4 objectSpacePosition = mul(componentSpacePosition, componentCoordinateToObjectTransform);
	float4 worldSpacePosition = mul(objectSpacePosition, objectCoordinateToWorldTransform);

	float3 componentSpaceNormal = vIn.normal;
	float3 objectSpaceNormal = mul(componentSpaceNormal, componentNormalToObjectTransform);
	float3 worldSpaceNormal = mul(objectSpaceNormal, objectNormalToWorldTransform);

	vOut.positions = calculatePositions(worldSpacePosition.xyz);
	vOut.normal = worldSpaceNormal;
	
	vOut.tangent = 0;
	vOut.texcoord = vIn.texcoord;

	vOut.secondaryTangent = 0;
	vOut.secondaryTexcoord = 0;

	vOut.occlusion = 0;
	vOut.scatteredIllumination = 0;

	return vOut;
}
