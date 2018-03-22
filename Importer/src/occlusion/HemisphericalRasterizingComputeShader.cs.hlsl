#include "HemisphericalRasterizingCommon.hlsl"
#include <subdivision/BasicRefinedVertexInfo.hlsl>

static const int THREAD_COUNT = 32;

typedef uint4 Quad;

//Precomputed data
StructuredBuffer<RasterFull> rasterCube : register(t0);

//Mesh data
StructuredBuffer<Quad> faces : register(t1);
StructuredBuffer<float> transparencies : register(t2);
StructuredBuffer<uint> vertexMasks : register(t3);
StructuredBuffer<uint> faceMasks : register(t4);
StructuredBuffer<BasicRefinedVertexInfo> vertexInfos : register(t5);

struct ArraySegment {
	uint offset;
	uint count;
};

//Dispatch data
cbuffer cbuffer0 : register(b1) {
	ArraySegment segment;
};

//Output
RWStructuredBuffer<float2> occlusions : register(u0);

RasterFull rasterizePlaneThroughOrigin(float3 planeNormal) {
	uint faceIdx, sIdx, tIdx;
	bool invert;
	CubeMap_LocationFromVector(planeNormal, faceIdx, sIdx, tIdx, invert);

	uint flatIdx = CubeMap_ToFlatIdx(faceIdx, sIdx, tIdx);
	RasterFull raster = rasterCube[flatIdx];

	if (invert) {
		RasterFull_InplaceNot(raster);
	}
	
	return raster;
}

RasterFull rasterizeEdgePlane(float3 vertexA, float3 vertexB, bool isBackface) {
	float3 planeNormal = cross(vertexB, vertexA);
	if (isBackface) {
		planeNormal *= -1;
	}
	return rasterizePlaneThroughOrigin(planeNormal);
}

bool Quad_Contains(Quad face, uint vertexIdx) {
	return face[0] == vertexIdx || face[1] == vertexIdx || face[2] == vertexIdx || face[3] == vertexIdx;
}

RasterFull rasterizeFace(float4x4 viewTransform, int faceIdx, int skipVertexIdx) {
	Quad face = faces[faceIdx];

	float3 vert0 = (float3) mul(float4(vertexInfos[face[0]].position, 1), viewTransform);
	float3 vert1 = (float3) mul(float4(vertexInfos[face[1]].position, 1), viewTransform);
	float3 vert2 = (float3) mul(float4(vertexInfos[face[2]].position, 1), viewTransform);
	float3 vert3 = (float3) mul(float4(vertexInfos[face[3]].position, 1), viewTransform);

	float3 faceNormal = cross(vert1 - vert0, vert2 - vert1);

	if ((vertexMasks[skipVertexIdx] & faceMasks[faceIdx]) != 0) {
		return (RasterFull) 0;
	} else if (Quad_Contains(face, skipVertexIdx)) {
		return (RasterFull) 0;
	} else if (vert0.z >= 0 && vert1.z >= 0 && vert2.z >= 0 && vert3.z >= 0) {
		//behind camera
		return (RasterFull) 0;
	}
	else {
		bool isBackface = dot(faceNormal, vert1) > 0;
		RasterFull faceRaster = rasterizeEdgePlane(vert0, vert1, isBackface);
		RasterFull_AndEquals(faceRaster, rasterizeEdgePlane(vert1, vert2, isBackface));
		RasterFull_AndEquals(faceRaster, rasterizeEdgePlane(vert2, vert3, isBackface));
		RasterFull_AndEquals(faceRaster, rasterizeEdgePlane(vert3, vert0, isBackface));
		return faceRaster;
	}
}

struct FloatRasterRow {
	float values[RASTER_COL_COUNT];
};

uint roundUp(uint num, uint divisor) {
	return (num + divisor - 1) / divisor;
}

groupshared float perFaceTransparencies[THREAD_COUNT];
groupshared uint perFaceRasters[RASTER_ROW_COUNT][THREAD_COUNT];

void rasterizeMesh(out FloatRasterRow meshRaster, uint groupIdx, uint rowIdx, float4x4 viewTransform, int skipVertexIdx) {
	uint faceCount, faceStride;
	faces.GetDimensions(faceCount, faceStride);
	uint faceGroupCount = roundUp(faceCount, RASTER_ROW_COUNT);

	meshRaster = (FloatRasterRow) 1;

	for (uint faceGroupIdx = 0; faceGroupIdx < faceGroupCount; ++faceGroupIdx) {
		//thread group spread
		{
			uint faceOffset = rowIdx;
			uint faceIdx = faceGroupIdx * RASTER_ROW_COUNT + faceOffset;

			float transparency;
			RasterFull raster;

			if (faceIdx < faceCount) {
				transparency = transparencies[faceIdx];
				raster = rasterizeFace(viewTransform, faceIdx, skipVertexIdx);
			}
			else {
				transparency = 1;
				raster = (RasterFull) 0;
			}

			perFaceTransparencies[groupIdx] = transparency;

			[unroll]
			for (uint rowIdx = 0; rowIdx < RASTER_ROW_COUNT; ++rowIdx) {
				perFaceRasters[rowIdx][groupIdx] = raster.rows[rowIdx];
			}
		}

		GroupMemoryBarrierWithGroupSync();

		uint groupBase = groupIdx / RASTER_ROW_COUNT * RASTER_ROW_COUNT;
		for (uint faceOffset = 0; faceOffset < RASTER_ROW_COUNT; ++faceOffset) {
			RasterRow faceRasterRow = perFaceRasters[rowIdx][groupBase + faceOffset];
			float transparency = perFaceTransparencies[groupBase + faceOffset];

			[unroll]
			for (uint col = 0; col < RASTER_COL_COUNT; ++col) {
				bool isSet = (faceRasterRow & (1 << col)) != 0;
				meshRaster.values[col] *= isSet ? transparency : 1;
			}
		}
	}
}

float4x4 lookAtRH(float3 position, float3 viewDir, float3 up) {
	float3 zaxis = -viewDir;
	float3 xaxis = normalize(cross(up, zaxis));
	float3 yaxis = cross(zaxis, xaxis);

	float4x4 result;
	result[0][0] = xaxis.x; result[1][0] = xaxis.y; result[2][0] = xaxis.z;
	result[0][1] = yaxis.x; result[1][1] = yaxis.y; result[2][1] = yaxis.z;
	result[0][2] = zaxis.x; result[1][2] = zaxis.y; result[2][2] = zaxis.z;
	
	result[3][0] = -dot(xaxis, position);
	result[3][1] = -dot(yaxis, position);
	result[3][2] = -dot(zaxis, position);

	result[3][3] = 1;

	return result;
}

groupshared float4 perRowOccludedPointAndWeightSum[THREAD_COUNT];
groupshared float perRowAllWeightsSum[THREAD_COUNT];

float calculateOcclusionInfo(float3 viewPos, float3 viewDir, int groupIdx, int rowIdx, int receiverIdx) {
	float3 up = abs(viewDir.y) > abs(viewDir.x) && abs(viewDir.y) > abs(viewDir.z) ? float3(0, 0, 1) : float3(0, 1, 0);

	float4x4 viewTransform = lookAtRH(viewPos, viewDir, up);

	{
		float4 occludedPointAndWeightSum = 0;
		float allWeightsSum = 0;

		FloatRasterRow rowRaster;
		rasterizeMesh(rowRaster, groupIdx, rowIdx, viewTransform, receiverIdx);
		
		for (uint col = 0; col < RASTER_ROW_COUNT; ++col) {
			float4 pointAndWeight = hemispherePointsAndWeights[rowIdx * RASTER_ROW_COUNT + col];

			float opacity = 1 - rowRaster.values[col];
			occludedPointAndWeightSum += opacity * pointAndWeight;
			allWeightsSum += pointAndWeight.w;
		}

		perRowOccludedPointAndWeightSum[groupIdx] = occludedPointAndWeightSum;
		perRowAllWeightsSum[groupIdx] = allWeightsSum;
	}

	GroupMemoryBarrierWithGroupSync();

	if (rowIdx == 0) {
		float4 occludedPointAndWeightSum = 0;
		float allWeightsSum = 0;

		for (uint rowIdx2 = 0; rowIdx2 < RASTER_ROW_COUNT; ++rowIdx2) {
			occludedPointAndWeightSum += perRowOccludedPointAndWeightSum[groupIdx + rowIdx2];
			allWeightsSum += perRowAllWeightsSum[groupIdx + rowIdx2];
		}

		float4 occlusionInfo = occludedPointAndWeightSum / allWeightsSum;

		//transform occlusion direction from tangent space to object space
		//note that the usual vector-matrix multiplication order is swapped in order to invert the rotation
		occlusionInfo.xyz = mul((float3x3) viewTransform, occlusionInfo.xyz);

		return occlusionInfo.w;
	} else {
		return 0;
	}
}

[numthreads(RASTER_ROW_COUNT, THREAD_COUNT / RASTER_ROW_COUNT, 1)]
void main(uint groupIdx : SV_GroupIndex, uint3 dispatchThreadID : SV_DispatchThreadID) {
	uint rowIdx = dispatchThreadID.x;
	uint receiverIdx = segment.offset + dispatchThreadID.y;

	float3 viewPos = vertexInfos[receiverIdx].position;
	float3 viewDir = vertexInfos[receiverIdx].normal;

	float2 occlusion;
	occlusion[0] = calculateOcclusionInfo(viewPos, viewDir, groupIdx, rowIdx, receiverIdx);
	occlusion[1] = calculateOcclusionInfo(viewPos, -viewDir, groupIdx, rowIdx, receiverIdx);

	if (rowIdx == 0) {
		occlusions[receiverIdx] = occlusion;
	}
}
