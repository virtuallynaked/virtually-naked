#include <subdivision/BasicRefinedVertexInfo.hlsl>

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
StructuredBuffer<float3> controlVertexPositions : register(t2);

RWStructuredBuffer<BasicRefinedVertexInfo> refinedVertexInfos : register(u0);

BasicRefinedVertexInfo refine(uint vertexIdx) {
	float3 position = 0;
	float3 positionDu = 0;
	float3 positionDv = 0;
		
	ArraySegment arraySegment = stencilSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndexWithDerivative stencil = stencilElems[arraySegment.offset + i];
		float3 controlVertexPosition = controlVertexPositions[stencil.index];

		position += stencil.weight * controlVertexPosition;
		positionDu += stencil.duWeight * controlVertexPosition;
		positionDv += stencil.dvWeight * controlVertexPosition;
	}

	float3 normal = normalize(cross(positionDu, positionDv));

	BasicRefinedVertexInfo info;
	info.position = position;
	info.normal = normal;
	return info;
}

[numthreads(64, 1, 1)]
void main(uint idx : SV_DispatchThreadID) {
	BasicRefinedVertexInfo info = refine(idx);
	refinedVertexInfos[idx] = info;
}