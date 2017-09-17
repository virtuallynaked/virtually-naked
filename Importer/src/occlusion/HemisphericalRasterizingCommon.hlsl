static const uint RASTER_ROW_COUNT = 32;
static const uint RASTER_COL_COUNT = 32;

typedef uint RasterRow;

struct RasterFull {
	RasterRow rows[RASTER_ROW_COUNT];
};

void RasterFull_Set(out RasterFull lhs, uint row) {
	[unroll]
	for (uint rowIdx = 0; rowIdx < RASTER_ROW_COUNT; ++rowIdx) {
		lhs.rows[rowIdx] = row;
	}
}

void RasterFull_AndEquals(inout RasterFull lhs, RasterFull rhs) {
	[unroll]
	for (uint rowIdx = 0; rowIdx < RASTER_ROW_COUNT; ++rowIdx) {
		lhs.rows[rowIdx] &= rhs.rows[rowIdx];
	}
}

void RasterFull_InplaceNot(inout RasterFull lhs) {
	[unroll]
	for (uint rowIdx = 0; rowIdx < RASTER_ROW_COUNT; ++rowIdx) {
		lhs.rows[rowIdx] = ~lhs.rows[rowIdx];
	}
}

static const uint CUBE_MAP_DIM = 128;

uint floatToIdx(uint binCount, float f) {
	return clamp((uint)(f * binCount), 0, binCount - 1);
}

float idxToFloat(uint binCount, uint idx) {
	return (idx + 0.5f) / binCount;
}

uint CubeMap_ToFlatIdx(uint face, uint sIdx, uint tIdx) {
	return face * CUBE_MAP_DIM * CUBE_MAP_DIM + sIdx * CUBE_MAP_DIM + tIdx;
}

float3 CubeMap_VectorFromLocation(uint face, uint sIdx, uint tIdx, bool invert) {
	float s = idxToFloat(CUBE_MAP_DIM, sIdx) * 2 - 1;
	float t = idxToFloat(CUBE_MAP_DIM, tIdx) * 2 - 1;

	float3 v;
	if (face == 0) {
		v.x = +1;
		v.y = s;
		v.z = t;
	}
	else if (face == 1) {
		v.y = +1;
		v.x = s;
		v.z = t;
	}
	else {
		v.z = +1;
		v.x = s;
		v.y = t;
	}

	if (invert) {
		v = -v;
	}

	return v;
}

void CubeMap_LocationFromVector(float3 v, out uint face, out uint sIdx, out uint tIdx, out bool invert) {
	float absX = abs(v.x);
	float absY = abs(v.y);
	float absZ = abs(v.z);

	float s, t;
	if (absX >= absY && absX >= absZ) {
		face = 0;
		invert = v.x < 0;
		s = v.y / absX;
		t = v.z / absX;
	}
	else if (absY >= absX && absY >= absZ) {
		face = 1;
		invert = v.y < 0;
		s = v.x / absY;
		t = v.z / absY;
	}
	else {
		face = 2;
		invert = v.z < 0;
		s = v.x / absZ;
		t = v.y / absZ;
	}

	if (invert) {
		s *= -1;
		t *= -1;
	}

	sIdx = floatToIdx(CUBE_MAP_DIM, (s + 1) / 2);
	tIdx = floatToIdx(CUBE_MAP_DIM, (t + 1) / 2);
}

//Hemisphere data
cbuffer cbuffer0 : register(b0) {
	float4 hemispherePointsAndWeights[RASTER_ROW_COUNT * RASTER_ROW_COUNT];
};