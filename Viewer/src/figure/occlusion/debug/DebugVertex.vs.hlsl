#include <d3d/VertexCommon.hlsl>
#include <figure/shaping/shader/Quaternion.hlsl>
#include <figure/shaping/shader/ControlVertexInfo.hlsl>
#include <figure/shaping/shader/OcclusionSurrogate.hlsl>

StructuredBuffer<uint3> surrogateFaces : register(t0);
StructuredBuffer<uint> packedOcclusions: register(t1);

struct VertexIn {
	float3 position : POSITION;
	float3 normal : NORMAL;
};

VertexOutput main(uint vertexIdx : SV_VertexId, VertexIn vIn) {
	float3 worldPosition = vIn.position * 0.1 + float3(0, 1.5, 0);

	int offset = 18154 + 0 * 545;
	//int occlusionInfoIdx = vertexIdx + offset;
	//float2 occlusion = unpackOcclusion(packedOcclusions[occlusionInfoIdx]);

	float2 occlusion = occlusionFromNormal(surrogateFaces, packedOcclusions, offset, vIn.normal);

	VertexOutput vOut = (VertexOutput) 0;
	vOut.positions = calculatePositions(worldPosition);
	vOut.normal = vIn.normal;
	vOut.texcoord = float2(0.5, 0.5);
	vOut.occlusion = occlusion;
	return vOut;
}
