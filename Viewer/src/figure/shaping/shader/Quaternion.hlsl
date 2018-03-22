typedef float4 Quaternion;

Quaternion Quaternion_Identity() {
	return float4(0, 0, 0, 1);
}

Quaternion Quaternion_Zero() {
	return float4(0, 0, 0, 0);
}

float3 Quaternion_Apply(Quaternion q, float3 p) {
	return p + 2 * cross(cross(p, q.xyz) - q.w * p, q.xyz);
}
