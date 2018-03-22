float4 main(float4 screenPosition : SV_POSITION) : SV_TARGET {
	float3 color = 0.045 * float3(1, 1, 1);
	return float4(color, 1);
}
