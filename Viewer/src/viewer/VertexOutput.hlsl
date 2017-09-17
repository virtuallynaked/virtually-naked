struct VertexOutputPositions {
	float3 objectRelativeEyePosition : POSITION;
	float4 screenPosition : SV_POSITION;
};

struct VertexOutput {
	VertexOutputPositions positions;

	float3 normal : NORMAL;
	float3 tangent : TANGENT;

	float2 texcoord : TEXCOORD0;

	float2 occlusion : COLOR0;
	centroid float3 scatteredIllumination : COLOR1;
};
