struct VIn {
	float2 uv : TEXCOORD;
	float3 ldNormal : NORMAL0;
	float3 ldTangent : TANGENT0;
	float3 hdNormal : NORMAL1;
};

struct VOut {
	float4 pos : SV_POSITION;
	float3 ldNormal : NORMAL0;
	float3 ldTangent : TANGENT0;
	float3 hdNormal : NORMAL1;
};

float3 normalizeNonZero(float3 v) {
	float lengthSquared = dot(v, v);
	return lengthSquared == 0 ? v : v * rsqrt(lengthSquared);
}

VOut main(VIn vIn) {
	float2 uv = float2(vIn.uv.x % 1, vIn.uv.y % 1);

	VOut vOut;
	vOut.pos = float4(uv * 2 - 1, 0, 1);
	vOut.ldNormal = vIn.ldNormal;
	vOut.ldTangent = normalizeNonZero(vIn.ldTangent - vIn.ldNormal * dot(vIn.ldTangent, vIn.ldNormal));
	vOut.hdNormal = vIn.hdNormal;
	return vOut;
}
