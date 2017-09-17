RWTexture2D<unorm float4> tex : register(u0);

[numthreads(16, 16, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
	uint2 pixelId = dispatchThreadId.xy;

	float4 c = tex[pixelId];
	c.rgb *= c.a;
	tex[pixelId] = c;
}