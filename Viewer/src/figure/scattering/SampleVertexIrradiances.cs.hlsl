#include <figure/shaping/shader/ControlVertexInfo.hlsl>
#include <texturing/ImageBasedLightingCommon.hlsl>

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
};

StructuredBuffer<ArraySegment> stencilSegments : register(t2);
StructuredBuffer<WeightedIndexWithDerivative> stencilElems : register(t3);
StructuredBuffer<ControlVertexInfo> controlVertexInfos : register(t4);

RWStructuredBuffer<float3> irrandiancesOut : register(u0);

SpatialVertexInfo refineSpatialInfo(uint spatialIdx) {
	SpatialVertexInfo refinedVertexInfo;
	refinedVertexInfo.position = 0;
	refinedVertexInfo.positionDu = 0;
	refinedVertexInfo.positionDv = 0;
	refinedVertexInfo.occlusion = 0;
	float2 meanOcclusionF = 0;

	float exponent = 4;

	ArraySegment arraySegment = stencilSegments[spatialIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndexWithDerivative stencil = stencilElems[arraySegment.offset + i];
		ControlVertexInfo controlVertexInfo = controlVertexInfos[stencil.index];

		refinedVertexInfo.position += stencil.weight * controlVertexInfo.position;
		refinedVertexInfo.positionDu += stencil.duWeight * controlVertexInfo.position;
		refinedVertexInfo.positionDv += stencil.dvWeight * controlVertexInfo.position;

		float2 controlOcclusion = unpackOcclusion(controlVertexInfo.packedOcclusion);
		meanOcclusionF += stencil.weight * pow(controlOcclusion, 1 / exponent);
	}

	refinedVertexInfo.occlusion = pow(meanOcclusionF, exponent);
	return refinedVertexInfo;
}

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	int vertexIdx = dispatchThreadId.x;
	SpatialVertexInfo spatialInfo = refineSpatialInfo(vertexIdx);

	float3 positionDs = spatialInfo.positionDu;
	float3 positionDt = spatialInfo.positionDv;

	float3 normal = normalize(cross(positionDs, positionDt));
	float occlusionAmount = spatialInfo.occlusion[0];

	irrandiancesOut[vertexIdx] = sampleDiffuseIllumination(normal, occlusionAmount);
}
