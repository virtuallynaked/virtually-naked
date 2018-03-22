struct SurrogateInfo {
	float3 center;
	uint offset;
	Quaternion rotation;
};

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

float2 occlusionFromNormal(StructuredBuffer<uint3> surrogateFaces, StructuredBuffer<uint> packedOcclusions, int offset, float3 normal) {
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
