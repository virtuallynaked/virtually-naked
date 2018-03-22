#include <figure/shaping/shader/ControlVertexInfo.hlsl>
#include <texturing/ImageBasedLightingCommon.hlsl>

struct ArraySegment {
	uint offset;
	uint count;
};

struct WeightedIndex {
	uint index;
	float3 weight;
};

struct SpatialVertexInfo {
	float3 position;
	float3 positionDu;
	float3 positionDv;
	float2 occlusion;
};

StructuredBuffer<float3> transmitterIrradiances : register(t0);
StructuredBuffer<ArraySegment> formFactorSegments : register(t1);
StructuredBuffer<WeightedIndex> formFactorElems : register(t2);

RWStructuredBuffer<float3> irrandiancesOut : register(u0);

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	int vertexIdx = dispatchThreadId.x;

	float3 scatteredIrradiance = 0;

	ArraySegment arraySegment = formFactorSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndex formFactorElem = formFactorElems[arraySegment.offset + i];

		scatteredIrradiance += formFactorElem.weight * transmitterIrradiances[formFactorElem.index];
	}
	
	irrandiancesOut[vertexIdx] = scatteredIrradiance;
}
