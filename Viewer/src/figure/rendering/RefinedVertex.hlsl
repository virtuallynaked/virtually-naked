struct RefinedVertex {
	float3 position : POSITION;
	float3 positionDs : TANGENT;
	float3 positionDt : BINORMAL;

	float2 occlusion : COLOR0;
	float3 scatteredIllumination : COLOR1;
};
