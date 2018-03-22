Texture2DMS<float> depthTexture;

static const int SampleCount = 4;

float main(float4 screenPosition : SV_POSITION) : SV_DEPTH {
	float maxDepth = 0;
	for (int i = 0; i < SampleCount; ++i) {
		float depth = depthTexture.Load(screenPosition.xy, i);
		maxDepth = max(maxDepth, depth);
	}
	return maxDepth;
}
