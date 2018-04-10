struct TransparencyCounters {
	uint pixelCount;
	uint transparencyCount;
};

Texture2D<float> opacityTexture : register(t0);

RWStructuredBuffer<TransparencyCounters> counterArray : register(u0);

void main(float4 screenPosition : SV_POSITION, uint primitiveId : SV_PrimitiveID) {
	uint faceIdx = primitiveId / 2; //two triangles per face

	int2 loc = int2(screenPosition.xy);
	float opacity = opacityTexture[loc];
	float transparency = 1 - opacity;

	InterlockedAdd(counterArray[faceIdx].pixelCount, 1);
	InterlockedAdd(counterArray[faceIdx].transparencyCount, (uint) (0xff * transparency + 0.5));
}
