TextureCube cube : register(t0);
SamplerState cubeSampler : register(s0);

StructuredBuffer<float4> inBuffer : register(t1);
RWStructuredBuffer<float4> outBuffer : register(u0);

[numthreads(1, 1, 1)]
void main(uint dispatchThreadID : SV_DispatchThreadID) {
	float4 value = cube.SampleLevel(cubeSampler, inBuffer[dispatchThreadID].xyz, 0);
	outBuffer[dispatchThreadID] = value;
}