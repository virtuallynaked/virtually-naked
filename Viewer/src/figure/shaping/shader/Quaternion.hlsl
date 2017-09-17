typedef float4 Quaternion;

Quaternion Quaternion_Identity() {
	return float4(0, 0, 0, 1);
}

Quaternion Quaternion_Zero() {
	return float4(0, 0, 0, 0);
}