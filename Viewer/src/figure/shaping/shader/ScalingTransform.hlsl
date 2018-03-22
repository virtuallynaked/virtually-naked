struct ScalingTransform {
	row_major float3x3 scale;
	float3 translation;
};

ScalingTransform ScalingTransform_Identity() {
	ScalingTransform transform;
	transform.scale = float3x3(
		1, 0, 0,
		0, 1, 0,
		0, 0, 1);
	transform.translation = float3(0, 0, 0);
	return transform;
}

ScalingTransform ScalingTransform_Zero() {
	ScalingTransform transform;
	transform.scale = float3x3(
		0, 0, 0,
		0, 0, 0,
		0, 0, 0);
	transform.translation = float3(0, 0, 0);
	return transform;
}

void ScalingTransform_Accumulate(inout ScalingTransform accumulator, float weight, ScalingTransform transform) {
	accumulator.scale += weight * transform.scale;
	accumulator.translation += weight * transform.translation;
}

void ScalingTransform_FinishAccumulate(inout ScalingTransform accumulator) {
}

float3 ScalingTransform_Apply(ScalingTransform transform, float3 p) {
	return mul(p, transform.scale) + transform.translation;
}
