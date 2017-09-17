Texture2D sourceTexture : register(t0);

RWTexture2D<unorm float4> resultTexture : register(u0);

[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	uint2 pixelId = dispatchThreadId.xy;

	uint width, height, numberOfLevels;
	sourceTexture.GetDimensions(0, width, height, numberOfLevels);

	float4 c = 0;
	uint2 samplePixelId = pixelId;
	uint mipLevel = 0;
	while (mipLevel < numberOfLevels) {
		float4 samp = sourceTexture.mips[mipLevel][samplePixelId];

		c += samp * (1 - c.a);

		mipLevel += 1;
		samplePixelId /= 2;
	}
	
	c /= c.a;
	
	resultTexture[pixelId] = c;
}
