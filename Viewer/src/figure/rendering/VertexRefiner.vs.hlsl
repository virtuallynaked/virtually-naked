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
	uint spatialInfoIdx;
	float2 texCoord;
	float2 texCoordDu;
	float2 texCoordDv;
};

StructuredBuffer<ArraySegment> stencilSegments : register(t0);
StructuredBuffer<WeightedIndexWithDerivative> stencilElems : register(t1);
StructuredBuffer<TexturedVertexInfo> texturedVertexInfos : register(t2);
StructuredBuffer<ControlVertexInfo> controlVertexInfos : register(t3);
StructuredBuffer<float3> controlScatteredIlluminations : register(t4);

SpatialVertexInfo refineSpatialInfo(uint spatialIdx) {
	SpatialVertexInfo refinedVertexInfo;
	refinedVertexInfo.position = 0;
	refinedVertexInfo.positionDu = 0;
	refinedVertexInfo.positionDv = 0;
	refinedVertexInfo.occlusion = 0;
	refinedVertexInfo.scatteredIllumination = 0;
	
	ArraySegment arraySegment = stencilSegments[spatialIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndexWithDerivative stencil = stencilElems[arraySegment.offset + i];
		ControlVertexInfo controlVertexInfo = controlVertexInfos[stencil.index];
		float3 controlScatteredIllumination = controlScatteredIlluminations[stencil.index];

		refinedVertexInfo.position += stencil.weight * controlVertexInfo.position;
		refinedVertexInfo.positionDu += stencil.duWeight * controlVertexInfo.position;
		refinedVertexInfo.positionDv += stencil.dvWeight * controlVertexInfo.position;

		refinedVertexInfo.occlusion += stencil.weight * unpackOcclusion(controlVertexInfo.packedOcclusion);
		refinedVertexInfo.scatteredIllumination += stencil.weight * controlScatteredIllumination;
	}
	
	return refinedVertexInfo;
}

RefinedVertex combineTextureAndSpatialInfo(TexturedVertexInfo info, SpatialVertexInfo spatialInfo) {
	float3 positionDs = spatialInfo.positionDu;
	float3 positionDt = spatialInfo.positionDv;

	float3 normal = normalize(cross(positionDs, positionDt));

	float2 texCoordDs = info.texCoordDu;
	float2 texCoordDt = info.texCoordDv;

	float us = texCoordDs[0];
	float vs = texCoordDs[1];
	float ut = texCoordDt[0];
	float vt = texCoordDt[1];

	//a constant factor of (ut*vs - us*vt) is omitted since it will drop out when these results get normalized later
	float3 positionDu = (vs * positionDt - vt * positionDs);
	float3 positionDv = (ut * positionDs - us * positionDt);

	float3 positionDuLength = length(positionDu);
	float3 tangent = positionDuLength > 0 ? (positionDu / positionDuLength) : 0;

	// dot(tangent, spatialInfo.normal) should be zero at this point

	RefinedVertex refinedVertex;
	refinedVertex.position = spatialInfo.position;
	refinedVertex.normal = normal;
	refinedVertex.occlusion = spatialInfo.occlusion;
	refinedVertex.tangent = tangent;
	refinedVertex.texCoord = info.texCoord;
	refinedVertex.scatteredIllumination = spatialInfo.scatteredIllumination;
	return refinedVertex;
}

RefinedVertex main(uint vertexIdx : SV_VertexId) {
	TexturedVertexInfo info = texturedVertexInfos[vertexIdx];
	SpatialVertexInfo spatialInfo = refineSpatialInfo(info.spatialInfoIdx);
	return combineTextureAndSpatialInfo(info, spatialInfo);
}