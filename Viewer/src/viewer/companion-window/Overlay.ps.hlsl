Texture2D overlayTexture;

float4 main(float4 screenPosition : SV_POSITION) : SV_TARGET {
	int2 pos = int2(screenPosition.xy);
	float4 result = overlayTexture[pos];
	return result;
}
