struct ControlVertexInfo {
	float3 position;
	uint packedOcclusion;
};

float2 unpackOcclusion(uint packedOcclusion) {
	return float2(
		(((packedOcclusion >> 0) & 0xffff) + 0.5) / (float) 0x10000,
		(((packedOcclusion >> 16) & 0xffff) + 0.5) / (float) 0x10000);
}

uint toShort(float f) {
	f *= 0x10000;
	if (f < 0) {
		return 0;
	} else if (f >= (float) 0x10000) {
		return 0xffff;
	} else {
		return uint(f);
	}
}

uint packOcclusion(float2 occlusion) {
	uint lower = toShort(occlusion[0]);
	uint upper = toShort(occlusion[1]);

	return (upper << 16) | lower;
}
