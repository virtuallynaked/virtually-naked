#include "Quaternion.hlsl"

struct DualQuaternion {
	Quaternion real;
	Quaternion dual;
};

DualQuaternion DualQuaternion_Identity() {
	DualQuaternion q;
	q.real = Quaternion_Identity();
	q.dual = Quaternion_Zero();
	return q;
}

DualQuaternion DualQuaternion_Zero() {
	DualQuaternion q;
	q.real = Quaternion_Zero();
	q.dual = Quaternion_Zero();
	return q;
}

void DualQuaternion_Accumulate(inout DualQuaternion accumulator, float weight, DualQuaternion transform) {
	if (dot(accumulator.real, transform.real) < 0) {
		weight *= -1;
	}

	accumulator.real += weight * transform.real;
	accumulator.dual += weight * transform.dual;
}

void DualQuaternion_FinishAccumulate(inout DualQuaternion accumulator) {
	float recipLength = rsqrt(dot(accumulator.real, accumulator.real));
	accumulator.real *= recipLength;
	accumulator.dual *= recipLength;
}

float3 DualQuaternion_Apply(DualQuaternion dq, float3 p) {
	float3 t = 2 * (dq.real.w * dq.dual.xyz - dq.dual.w * dq.real.xyz + cross(dq.real.xyz, dq.dual.xyz));
	return p + 2 * cross(cross(p, dq.real.xyz) - dq.real.w * p, dq.real.xyz) + t;
}