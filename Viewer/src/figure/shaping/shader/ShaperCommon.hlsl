#include "ControlVertexInfo.hlsl"
#include "StagedSkinningTransform.hlsl"
#include "OcclusionSurrogate.hlsl"

struct ArraySegment {
	uint offset;
	uint count;
};

struct VertexDelta {
	uint morphIdx;
	float3 positionOffset;
};

struct WeightedIndex {
	uint index;
	float weight;
};

struct BoneWeight {
	uint boneIdx;
	float weight;
};

StructuredBuffer<float3> initialPositions : register(t0);

//morphing inputs
StructuredBuffer<ArraySegment> deltaSegments : register(t1);
StructuredBuffer<VertexDelta> deltaElems : register(t2);
StructuredBuffer<float> morphWeights : register(t3);

//automorphing inputs
StructuredBuffer<ArraySegment> baseDeltaWeightSegments : register(t4);
StructuredBuffer<WeightedIndex> baseDeltaWeightElems : register(t5);

//skinning inputs
StructuredBuffer<ArraySegment> boneWeightSegments : register(t6);
StructuredBuffer<BoneWeight> boneWeightElems : register(t7);
StructuredBuffer<StagedSkinningTransform> boneTransforms : register(t8);

//occlusion inputs
StructuredBuffer<uint> packedOcclusions : register(t9);
StructuredBuffer<uint> surrogateMap : register(t10);
StructuredBuffer<uint3> surrogateFaces : register(t11);
StructuredBuffer<SurrogateInfo> surrogateInfos : register(t12);

RWStructuredBuffer<ControlVertexInfo> vertexInfosOut : register(u0);

#if SHAPER_OUTPUT_DELTAS
RWStructuredBuffer<float3> baseDeltas : register(u1);
#else
StructuredBuffer<float3> baseDeltas : register(t13);
#endif

float3 calculateDelta(uint vertexIdx) {
	float3 totalDelta = 0;

	ArraySegment arraySegment = deltaSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		VertexDelta delta = deltaElems[arraySegment.offset + i];
		totalDelta += morphWeights[delta.morphIdx] * delta.positionOffset;
	}

	return totalDelta;
}

void morph(int vertexIdx, inout float3 p) {
	float3 delta = calculateDelta(vertexIdx);
	p += delta;
#if SHAPER_OUTPUT_DELTAS
	baseDeltas[vertexIdx] = delta;
#endif
}

void automorph(int vertexIdx, inout float3 p) {
	float3 delta = 0;
	ArraySegment arraySegment = baseDeltaWeightSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		WeightedIndex baseDeltaWeight = baseDeltaWeightElems[arraySegment.offset + i];
		delta += baseDeltaWeight.weight * baseDeltas[baseDeltaWeight.index];
	}

	p += delta;
}

void skin(int vertexIdx, inout float3 p) {
	StagedSkinningTransform transformAccumulator = StagedSkinningTransform_Zero();

	ArraySegment arraySegment = boneWeightSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		BoneWeight boneWeight = boneWeightElems[arraySegment.offset + i];
		StagedSkinningTransform_Accumulate(transformAccumulator, boneWeight.weight, boneTransforms[boneWeight.boneIdx]);
	}

	StagedSkinningTransform_FinishAccumulate(transformAccumulator);

	p = StagedSkinningTransform_Apply(transformAccumulator, p);
}

uint lookupOcclusion(uint vertexIdx, float3 unskinnedPosition) {
	uint surrogateIdxPlusOne = surrogateMap[vertexIdx];
	if (surrogateIdxPlusOne == 0) {

		return packedOcclusions[vertexIdx];
	} else {
		SurrogateInfo surrogateInfo = surrogateInfos[surrogateIdxPlusOne - 1];
		float3 normal = Quaternion_Apply(
			surrogateInfo.rotation,
			normalize(unskinnedPosition - surrogateInfo.center));
		float2 occlusion = occlusionFromNormal(surrogateFaces, packedOcclusions, surrogateInfo.offset, normal);
		return packOcclusion(occlusion);
	}
}

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	uint vertexIdx = dispatchThreadId.x;

	float3 position = initialPositions[vertexIdx];
	morph(vertexIdx, position);
	automorph(vertexIdx, position);
	uint packedOcclusion = lookupOcclusion(vertexIdx, position);
	skin(vertexIdx, position);

	ControlVertexInfo vertexInfo;
	vertexInfo.position = position;
	vertexInfo.packedOcclusion = packedOcclusion;

	vertexInfosOut[vertexIdx] = vertexInfo;
}