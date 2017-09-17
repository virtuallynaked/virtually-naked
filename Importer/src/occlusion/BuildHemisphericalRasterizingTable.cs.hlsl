#include "HemisphericalRasterizingCommon.hlsl"

RWStructuredBuffer<RasterFull> rasterCube : register(u0);

RasterFull rasterizePlaneThroughOrigin(float3 planeNormal) {
	RasterFull fullBits;

	[unroll]
	for (uint row = 0; row < RASTER_ROW_COUNT; ++row) {
		uint rowBits = 0;

		for (uint col = 0; col < RASTER_COL_COUNT; ++col) {
			float4 pointAndWeight = hemispherePointsAndWeights[row * RASTER_ROW_COUNT + col];

			bool isAbove = dot(pointAndWeight.xyz, planeNormal) >= 0;

			if (isAbove) {
				rowBits |= (1 << col);
			}
		}

		fullBits.rows[row] = rowBits;
	}

	return fullBits;
}

[numthreads(64, 1, 1)]
void main(uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint tIdx = dispatchThreadID.x;
	uint sIdx = dispatchThreadID.y;
	uint face = dispatchThreadID.z;

	float3 v = CubeMap_VectorFromLocation(face, sIdx, tIdx, false);
	rasterCube[CubeMap_ToFlatIdx(face, sIdx, tIdx)] = rasterizePlaneThroughOrigin(v);
}