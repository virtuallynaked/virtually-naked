float4 main(float2 position : POSITION) : SV_POSITION {
	return float4(position * 2 - 1, 0, 1);
}
