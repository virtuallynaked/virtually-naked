float4 main(float2 pos : POSITION0) : SV_POSITION {
	float2 texcoord = float2(pos.x % 1, pos.y % 1);
	return float4(texcoord * 2 - 1, 0, 1);
}