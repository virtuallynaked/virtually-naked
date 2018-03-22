float4 main(float2 pos : POSITION0) : SV_POSITION {
	pos = float2(pos.x % 1, pos.y % 1);
	return float4(pos * 2 - 1, 0, 1);
}
