#include <d3d/VertexCommon.hlsl>
#include "OcclusionSurrogate.hlsl"

float2 occlusionFromNormal(int offset, float3 normal) {
	int faceIdx;
	float2 coords;
	normalToControlFaceAndCoords(normal, faceIdx, coords);
	
	subdivide(faceIdx, coords);
	subdivide(faceIdx, coords);
	subdivide(faceIdx, coords);
	subdivide(faceIdx, coords);

	uint3 surrogateFace = surrogateFaces[faceIdx];
	uint3 occlusionIdxs = surrogateFace + offset;

	float2 occlusion = (1 - coords.x - coords.y) * unpackOcclusion(packedOcclusions[occlusionIdxs[0]])
		+ coords.x * unpackOcclusion(packedOcclusions[occlusionIdxs[1]])
	    + coords.y * unpackOcclusion(packedOcclusions[occlusionIdxs[2]]);
	return occlusion;
}

struct VertexIn {
	float3 position : POSITION;
	float3 normal : NORMAL;
};

VertexOutput main(uint vertexIdx : SV_VertexId, VertexIn vIn) {
	float3 worldPosition = vIn.position * 0.1 + float3(0, 1.5, 0);

	int offset = 18154 + 0 * 545;
	//int occlusionInfoIdx = vertexIdx + offset;
	//float2 occlusion = unpackOcclusion(packedOcclusions[occlusionInfoIdx]);

	float2 occlusion = occlusionFromNormal(offset, vIn.normal);

	VertexOutput vOut = (VertexOutput) 0;
	vOut.positions = calculatePositions(worldPosition);
	vOut.normal = vIn.normal;
	vOut.texcoord = float2(0.5, 0.5);
	vOut.occlusion = occlusion;
	return vOut;
}