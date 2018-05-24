#include <d3d/VertexCommon.hlsl>
#include "RefinedVertex.hlsl"

cbuffer bar : register(b1) {
	float4x4 worldMatrix;
	float3x3 worldMatrixInverseTranspose;
}

struct TexturedVertexInfo {
	float2 texCoord;
	float2 tangentUCoeffs;
};

struct TexturedVertexInfoPair {
	TexturedVertexInfo primary;
	TexturedVertexInfo secondary;
};

StructuredBuffer<TexturedVertexInfoPair> texturedVertexInfos : register(t0);

float2 convertTexcoord(float2 dazTexCoord) {
	return float2(dazTexCoord.x % 1, 1 - dazTexCoord.y);
}

float3 normalizeNonZero(float3 v) {
	float lengthSquared = dot(v, v);
	return lengthSquared == 0 ? v : v * rsqrt(lengthSquared);
}

VertexOutput main(uint vertexIdx : SV_VertexId, RefinedVertex vIn) {
	VertexOutput vOut;

	float3 worldPosition = mul(float4(vIn.position, 1), worldMatrix).xyz;
	vOut.positions = calculatePositions(worldPosition);

	float3 objectNormal = normalize(cross(vIn.positionDs, vIn.positionDt));
	vOut.normal = mul(objectNormal, worldMatrixInverseTranspose);

	TexturedVertexInfoPair texturedVertexInfoPair = texturedVertexInfos[vertexIdx];

	TexturedVertexInfo info = texturedVertexInfoPair.primary;
	float2 texcoord = info.texCoord;
	float3 tangent = normalizeNonZero(info.tangentUCoeffs.x * vIn.positionDs + info.tangentUCoeffs.y * vIn.positionDt);
	vOut.tangent = mul(tangent, worldMatrixInverseTranspose);
	vOut.texcoord = convertTexcoord(texcoord);

	TexturedVertexInfo secondaryInfo = texturedVertexInfoPair.secondary;
	float2 secondaryTexcoord = secondaryInfo.texCoord;
	float3 secondaryTangent = normalizeNonZero(secondaryInfo.tangentUCoeffs.x * vIn.positionDs + secondaryInfo.tangentUCoeffs.y * vIn.positionDt);
	vOut.secondaryTangent = mul(secondaryTangent, worldMatrixInverseTranspose);
	vOut.secondaryTexcoord = convertTexcoord(secondaryTexcoord);
	
	vOut.occlusion = vIn.occlusion;
	vOut.scatteredIllumination = vIn.scatteredIllumination;

	return vOut;
}
