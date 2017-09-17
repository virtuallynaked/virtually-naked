#include "../../shaping/shader/ControlVertexInfo.hlsl"

struct ArraySegment {
	uint offset;
	uint count;
};

struct OcclusionDelta {
	uint morphIdx;
	uint packedOcclusion;
};

StructuredBuffer<uint> packedWithoutChildrenOcclusions : register(t0);
StructuredBuffer<uint> packedWithChildrenOcclusions : register(t1);
StructuredBuffer<uint> packedBaseOcclusions : register(t2);
StructuredBuffer<float> weights : register(t3);
StructuredBuffer<ArraySegment> deltaSegments : register(t4);
StructuredBuffer<OcclusionDelta> deltaElems : register(t5);

RWStructuredBuffer<uint> packedOcclusionsOut : register(u0);

float2 trilerp(float2 x, float2 y, float2 z, float2 s) {
	float2 a = x;
	float2 b = -3 * x + 4 * y - z;
	float2 c = 2 * (x - 2 * y + z);
	return a + s*(b + c*s);
}

float2 log1p(float2 x) {
	return log(1 + x);
}

float2 newtonStep(float2 x, float2 alpha, float2 z) {
	float2 expx = exp(x);
	float2 numer1 = 1 + expx;
	float2 numer2 = z + lerp(-log1p(expx), log1p(1/expx), alpha);
	float2 denom = expx * (1 - alpha) + alpha;
	float2 step = x + numer1 * numer2 / denom;
	return step;
}

float2 solveRevealage(
	float2 baseRevealage, float2 baseOcclusion,
	float2 revealageProduct, float2 occlusionProduct
	) {
	float2 alpha = log(baseOcclusion) / (log(baseOcclusion) + log(baseRevealage));
	float2 z = lerp(-log(occlusionProduct), log(revealageProduct), alpha);

	float2 x = float2(0, 0);
	x = newtonStep(x, alpha, z);
	x = newtonStep(x, alpha, z);
	x = newtonStep(x, alpha, z);

	float2 combinedRevealage = 1 / (1 + exp(-x));
	return combinedRevealage;
}

float2 fastSolveRevealage(
	float2 baseRevealage, float2 baseOcclusion,
	float2 revealageProduct, float2 occlusionProduct
	) {
	float2 combinedRevealage0 = 1 - occlusionProduct;
	float2 combinedRevealage1 = revealageProduct / (revealageProduct + occlusionProduct);
	float2 combinedRevealage2 = revealageProduct;

	float2 combinedRevealage = trilerp(
		saturate(combinedRevealage0),
		saturate(combinedRevealage1),
		saturate(combinedRevealage2), baseRevealage);
	return combinedRevealage;
}

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	int vertexIdx = dispatchThreadId.x;

	float2 withoutChildrenOcclusion = unpackOcclusion(packedWithoutChildrenOcclusions[vertexIdx]);
	float2 withChildrenOcclusion = unpackOcclusion(packedWithChildrenOcclusions[vertexIdx]);
	float2 baseOcclusion = unpackOcclusion(packedBaseOcclusions[vertexIdx]);
	
	float2 withoutChildrenRevealage = 1 - withoutChildrenOcclusion;
	float2 withChildrenRevealage = 1 - withChildrenOcclusion;
	float2 baseRevealage = 1 - baseOcclusion;

	float2 occlusionProduct = 1;
	float2 revealageProduct = 1;

	occlusionProduct *= withChildrenOcclusion / withoutChildrenOcclusion;
	revealageProduct *= withChildrenRevealage / withoutChildrenRevealage;
		
	ArraySegment arraySegment = deltaSegments[vertexIdx];
	for (uint i = 0; i < arraySegment.count; ++i) {
		OcclusionDelta delta = deltaElems[arraySegment.offset + i];
		float weight = weights[delta.morphIdx];

		float2 morphOcclusion = unpackOcclusion(delta.packedOcclusion);
		float2 weightedMorphOcclusion = lerp(baseOcclusion, morphOcclusion, weight);
		float2 weightedMorphRevealage = 1 - weightedMorphOcclusion;

		/*
			Note: The normalization by baseRevealage/baseOcclusion here doesn't affect the
			result mathmatically, but helps with numerical precision.

			An even better solution would be to accumulate a sum of logs.
		*/
		occlusionProduct *= weightedMorphOcclusion / baseOcclusion;
		revealageProduct *= weightedMorphRevealage / baseRevealage;
	}
	
	float2 combinedRevealage = solveRevealage(
		baseRevealage, baseOcclusion,
		revealageProduct, occlusionProduct);
	float2 combinedOcclusion = 1 - combinedRevealage;

	packedOcclusionsOut[vertexIdx] = packOcclusion(combinedOcclusion);
}