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

StructuredBuffer<ArraySegment> stencilSegments : register(t0);
StructuredBuffer<WeightedIndexWithDerivative> stencilElems : register(t1);
StructuredBuffer<uint> texturedToSpatialIdxMap : register(t2);
StructuredBuffer<ControlVertexInfo> controlVertexInfos : register(t3);
StructuredBuffer<float3> controlScatteredIlluminations : register(t4);

RefinedVertex main(uint vertexIdx : SV_VertexId) {
	int spatialVertexIdx = texturedToSpatialIdxMap[vertexIdx];

	RefinedVertex refinedVertexInfo;
	refinedVertexInfo.position = 0;
	refinedVertexInfo.positionDs = 0;
	refinedVertexInfo.positionDt = 0;
	float2 refinedFourthRootOcclusion = 0;
	refinedVertexInfo.scatteredIllumination = 0;

	ArraySegment arraySegment = stencilSegments[spatialVertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndexWithDerivative stencil = stencilElems[arraySegment.offset + i];
		ControlVertexInfo controlVertexInfo = controlVertexInfos[stencil.index];
		float3 controlScatteredIllumination = controlScatteredIlluminations[stencil.index];

		refinedVertexInfo.position += stencil.weight * controlVertexInfo.position;
		refinedVertexInfo.positionDs += stencil.duWeight * controlVertexInfo.position;
		refinedVertexInfo.positionDt += stencil.dvWeight * controlVertexInfo.position;
		refinedFourthRootOcclusion += stencil.weight * unpackUIntToFloat2(controlVertexInfo.packedFourthRootOcclusion);
		refinedVertexInfo.scatteredIllumination += stencil.weight * controlScatteredIllumination;
	}

	float2 refinedSquareRootOcclusion = refinedFourthRootOcclusion * refinedFourthRootOcclusion;
	float2 refinedOcclusion = refinedSquareRootOcclusion * refinedSquareRootOcclusion;
	refinedVertexInfo.occlusion = refinedOcclusion;

	return refinedVertexInfo;
}
