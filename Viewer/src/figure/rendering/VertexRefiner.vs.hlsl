#include <figure/shaping/shader/ControlVertexInfo.hlsl>
#include "RefinedVertex.hlsl"

struct ArraySegment {
	uint offset;
	uint count;
};

struct WeightedIndexWithDerivative {
	uint index;
	float weight;
	float duWeight;
	float dvWeight;
};

struct SpatialVertexInfo {
	float3 position;
	float3 positionDu;
	float3 positionDv;
	float2 occlusion;
	float3 scatteredIllumination;
};

struct TexturedVertexInfo {
	float2 texCoord;
	float2 tangentUCoeffs;
};

StructuredBuffer<ArraySegment> stencilSegments : register(t0);
StructuredBuffer<WeightedIndexWithDerivative> stencilElems : register(t1);
StructuredBuffer<uint> texturedToSpatialIdxMap : register(t2);
StructuredBuffer<TexturedVertexInfo> primaryTexturedVertexInfos : register(t3);
StructuredBuffer<TexturedVertexInfo> secondaryTexturedVertexInfos : register(t4);
StructuredBuffer<ControlVertexInfo> controlVertexInfos : register(t5);
StructuredBuffer<float3> controlScatteredIlluminations : register(t6);

SpatialVertexInfo refineSpatialInfo(uint spatialIdx) {
	SpatialVertexInfo refinedVertexInfo;
	refinedVertexInfo.position = 0;
	refinedVertexInfo.positionDu = 0;
	refinedVertexInfo.positionDv = 0;
	float2 refinedFourthRootOcclusion = 0;
	refinedVertexInfo.scatteredIllumination = 0;
	
	ArraySegment arraySegment = stencilSegments[spatialIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndexWithDerivative stencil = stencilElems[arraySegment.offset + i];
		ControlVertexInfo controlVertexInfo = controlVertexInfos[stencil.index];
		float3 controlScatteredIllumination = controlScatteredIlluminations[stencil.index];

		refinedVertexInfo.position += stencil.weight * controlVertexInfo.position;
		refinedVertexInfo.positionDu += stencil.duWeight * controlVertexInfo.position;
		refinedVertexInfo.positionDv += stencil.dvWeight * controlVertexInfo.position;
		refinedFourthRootOcclusion += stencil.weight * unpackUIntToFloat2(controlVertexInfo.packedFourthRootOcclusion);
		refinedVertexInfo.scatteredIllumination += stencil.weight * controlScatteredIllumination;
	}
	
	float2 refinedSquareRootOcclusion = refinedFourthRootOcclusion * refinedFourthRootOcclusion;
	float2 refinedOcclusion = refinedSquareRootOcclusion * refinedSquareRootOcclusion;
	refinedVertexInfo.occlusion = refinedOcclusion;

	return refinedVertexInfo;
}

float3 normalizeNonZero(float3 v) {
	float lengthSquared = dot(v, v);
	return lengthSquared == 0 ? v : v * rsqrt(lengthSquared);
}

RefinedVertex combineTextureAndSpatialInfo(TexturedVertexInfo info, TexturedVertexInfo secondaryInfo, SpatialVertexInfo spatialInfo) {
	float3 positionDs = spatialInfo.positionDu;
	float3 positionDt = spatialInfo.positionDv;

	float3 normal = normalize(cross(positionDs, positionDt));

	float2 texCoord = info.texCoord;
	float3 tangent = normalizeNonZero(info.tangentUCoeffs.x * positionDs + info.tangentUCoeffs.y * positionDt);

	float2 secondaryTexCoord = secondaryInfo.texCoord;
	float3 secondaryTangent = normalizeNonZero(secondaryInfo.tangentUCoeffs.x * positionDs + secondaryInfo.tangentUCoeffs.y * positionDt);

	RefinedVertex refinedVertex;
	refinedVertex.position = spatialInfo.position;
	refinedVertex.normal = normal;
	refinedVertex.occlusion = spatialInfo.occlusion;
	refinedVertex.tangent = tangent;
	refinedVertex.texCoord = info.texCoord;
	refinedVertex.secondaryTangent = secondaryTangent;
	refinedVertex.secondaryTexCoord = secondaryTexCoord;
	refinedVertex.scatteredIllumination = spatialInfo.scatteredIllumination;
	return refinedVertex;
}

RefinedVertex main(uint vertexIdx : SV_VertexId) {
	int spatialVertexIdx = texturedToSpatialIdxMap[vertexIdx];
	SpatialVertexInfo spatialInfo = refineSpatialInfo(spatialVertexIdx);

	TexturedVertexInfo primaryTexturedInfo = primaryTexturedVertexInfos[vertexIdx];
	TexturedVertexInfo secondaryTexturedInfo = secondaryTexturedVertexInfos[vertexIdx];
	return combineTextureAndSpatialInfo(primaryTexturedInfo, secondaryTexturedInfo, spatialInfo);
}
