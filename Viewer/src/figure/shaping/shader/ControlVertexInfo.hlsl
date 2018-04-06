struct ControlVertexInfo {
	float3 position;
	uint packedFourthRootOcclusion;
};

float2 unpackUIntToFloat2(uint packed) {
	return float2(
		f16tof32(packed >> 0),
		f16tof32(packed >> 16));
}

uint packFloat2toUInt(float2 unpacked) {
	uint lower = f32tof16(unpacked[0]);
	uint upper = f32tof16(unpacked[1]);
	return (upper << 16) | lower;
}

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
