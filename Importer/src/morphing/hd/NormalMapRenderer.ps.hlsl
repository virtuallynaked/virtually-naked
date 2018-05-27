struct PIn {
	float4 pos : SV_POSITION;
	float3 ldNormal : NORMAL0;
	float3 ldTangent : TANGENT0;
	float3 hdNormal : NORMAL1;
};

float3 normalizeNonZero(float3 v) {
	float lengthSquared = dot(v, v);
	return lengthSquared == 0 ? v : v * rsqrt(lengthSquared);
}

float4 main(PIn pIn) : SV_TARGET {
	float3 ldNormal = normalize(pIn.ldNormal);
	float3 hdNormal = normalize(pIn.hdNormal);

	float3 ldTangent = normalizeNonZero(pIn.ldTangent - ldNormal * dot(pIn.ldTangent, ldNormal));
	float3 ldBitangent = -cross(ldTangent, ldNormal);

	float3x3 ldTbn = float3x3(ldTangent, ldBitangent, ldNormal);
	float3x3 inverseLdTbn = transpose(ldTbn); //inverse of an orthonormal matrix is its transpose

	float3 tangentSpaceHdNormal = mul(hdNormal, inverseLdTbn);

	return float4(0.5 + 0.5 * tangentSpaceHdNormal, 1);
}
