#include "../../shaping/shader/ControlVertexInfo.hlsl"

StructuredBuffer<uint3> surrogateFaces : register(t0);
StructuredBuffer<uint> packedOcclusions: register(t1);

void normalToControlFaceAndCoords(float3 normal, out int faceIdx, out float2 coords) {
	float3 absNormal = abs(normal);
	coords = absNormal.xy / (absNormal.x + absNormal.y + absNormal.z);

	if (normal.y >= 0) {
		if (normal.x >= 0) {
			faceIdx = 0;
		}
		else {
			faceIdx = 1;
			coords.xy = coords.yx;
		}
	}
	else {
		if (normal.x >= 0) {
			faceIdx = 3;
			coords.xy = coords.yx;
		}
		else {
			faceIdx = 2;
		}
	}
}

void subdivide(inout int faceIdx, inout float2 coords) {
	if (coords.x + coords.y < 0.5) {
		faceIdx = faceIdx * 4 + 0;
		coords = coords * 2;
	}
	else if (coords.x > 0.5) {
		faceIdx = faceIdx * 4 + 1;
		coords = float2(coords.x * 2 - 1, coords.y * 2);
	}
	else if (coords.y > 0.5) {
		faceIdx = faceIdx * 4 + 2;
		coords = float2(coords.x * 2, coords.y * 2 - 1);
	}
	else {
		faceIdx = faceIdx * 4 + 3;
		coords = float2(1 - coords.x * 2, 1 - coords.y * 2);
	}
}