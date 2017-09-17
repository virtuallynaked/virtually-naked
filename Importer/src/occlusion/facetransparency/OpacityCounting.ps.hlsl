struct OpacityCounters {
	uint pixelCount;
	uint opacityCount;
};

Texture2D<float> opacityTexture : register(t0);

RWStructuredBuffer<OpacityCounters> counterArray : register(u0);

void main(float4 screenPosition : SV_POSITION, uint primitiveId : SV_PrimitiveID) {
	uint faceIdx = primitiveId / 2; //two triangles per face

	int2 loc = int2(screenPosition.xy);
	float opacity = opacityTexture[loc];

	InterlockedAdd(counterArray[faceIdx].pixelCount, 1);
	InterlockedAdd(counterArray[faceIdx].opacityCount, (uint) (0xff * opacity + 0.5));
}