struct RefinedVertex {
	float3 position : POSITION;
	float3 normal : NORMAL;

	float2 occlusion : COLOR0;

	float3 tangent: TANGENT;
	float2 texCoord: TEXCOORD0;

	float3 scatteredIllumination : COLOR1;
};
