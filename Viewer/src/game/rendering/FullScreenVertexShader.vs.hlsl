float4 main(uint vI : SV_VERTEXID) : SV_POSITION {
	float2 zeroOneCoord = float2(vI & 1, vI >> 1);
	float4 screenPosition = float4(zeroOneCoord.x * 2 - 1, -(zeroOneCoord.y * 2 - 1), 0, 1);
	return screenPosition;
}
