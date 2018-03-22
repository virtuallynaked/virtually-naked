#include "RenderingConstants.hlsl"

Texture2D<float4> accumTexture : register(t0);
Texture2D<float> revealageTexture : register(t1);

Texture2D<float> depthTexture : register(t2);

Texture2DMS<float3> colorMsTexture : register(t3);
Texture2DMS<float> depthMsTexture : register(t4);

static const int SampleCount = 4;

static const float DepthEpsilon = 0.01; //in meters
bool isBehindReference(float referenceDepth, float depth) {
	return depth > referenceDepth + DepthEpsilon;
}

// Converts to from depth-buffer values to world-unit depth. Positive depths are further away.
float unprojectZ(float zproj) {
	return (zfar * znear) / (zfar - zfar * zproj + znear * zproj);
}

float4 calculateTransparency(int2 loc) {
	float4 accumSample = accumTexture[loc];
	float3 color = accumSample.rgb / max(accumSample.a, 1e-6);
	float opacity = 1 - revealageTexture[loc];
	return float4(color * opacity, opacity);
}

void accumulateBackNeighbourTransparency(float referenceDepth, int2 loc, inout uint count, inout float4 totalTransparency) {
	float depth = unprojectZ(depthTexture[loc]);
	if (!isBehindReference(referenceDepth, depth)) {
		return;
	}
	
	count += 1;
	totalTransparency += calculateTransparency(loc);
}

float4 calculateBackTransparency(float referenceDepth, int2 loc) {
	uint count = 0;
	float4 totalTransparency = (float4) 0;

	accumulateBackNeighbourTransparency(referenceDepth, int2(loc.x - 1, loc.y), count, totalTransparency);
	accumulateBackNeighbourTransparency(referenceDepth, int2(loc.x + 1, loc.y), count, totalTransparency);
	accumulateBackNeighbourTransparency(referenceDepth, int2(loc.x, loc.y - 1), count, totalTransparency);
	accumulateBackNeighbourTransparency(referenceDepth, int2(loc.x, loc.y + 1), count, totalTransparency);

	if (count == 0) {
		return (float4) 0;
	}
	else {
		return totalTransparency / count;
	}
}

float4 main(float4 screenPosition : SV_POSITION) : SV_Target {
	int2 loc = int2(screenPosition.xy);

	float referenceDepth = unprojectZ(depthTexture[screenPosition.xy]);
		
	float3 totalFrontMsColor = (float3) 0;
	uint frontCount = 0;

	float3 totalBackMsColor = (float3) 0;
	uint backCount = 0;
	
	[unroll]
	for (int i = 0; i < SampleCount; ++i) {
		float3 msColor = colorMsTexture.sample[i][loc];
		float msDepth = unprojectZ(depthMsTexture.sample[i][loc]);

		if (!isBehindReference(referenceDepth, msDepth)) {
			totalFrontMsColor += msColor;
			frontCount += 1;
		}
		else {
			totalBackMsColor += msColor;
			backCount += 1;
		}
	}

	float4 frontTransparency = calculateTransparency(loc);
	float3 result = totalFrontMsColor * (1 - frontTransparency.a) + frontCount * frontTransparency.rgb;
	
	if (backCount > 0) {
		float4 backTransparency = calculateBackTransparency(referenceDepth, loc);

		float4 backTransparencyExtra;
		if (backTransparency.a <= frontTransparency.a) {
			backTransparencyExtra = (float4) 0;
		} else {
			float backTransparencyExtraOpacity = backTransparency.a - frontTransparency.a;
			float3 backTransparencyColor = backTransparency.rgb / backTransparency.a;
			backTransparencyExtra = float4(backTransparencyColor * backTransparencyExtraOpacity, backTransparencyExtraOpacity);
		}
		float4 transparency = frontTransparency + backTransparencyExtra;

		result += totalBackMsColor * (1 - transparency.a) + backCount * transparency.rgb;
	}
	
	result /= SampleCount;
	return float4(result, 1);
}
