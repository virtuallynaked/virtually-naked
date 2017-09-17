#include "ScalingTransform.hlsl"
#include "DualQuaternion.hlsl"

struct StagedSkinningTransform {
	ScalingTransform scalingStage;
	DualQuaternion rotationStage;
};

StagedSkinningTransform StagedSkinningTransform_Identity() {
	StagedSkinningTransform transform;
	transform.scalingStage = ScalingTransform_Identity();
	transform.rotationStage = DualQuaternion_Identity();
	return transform;
}

StagedSkinningTransform StagedSkinningTransform_Zero() {
	StagedSkinningTransform transform;
	transform.scalingStage = ScalingTransform_Zero();
	transform.rotationStage = DualQuaternion_Zero();
	return transform;
}

void StagedSkinningTransform_Accumulate(inout StagedSkinningTransform accumulator, float weight, StagedSkinningTransform transform) {
	ScalingTransform_Accumulate(accumulator.scalingStage, weight, transform.scalingStage);
	DualQuaternion_Accumulate(accumulator.rotationStage, weight, transform.rotationStage);
}

void StagedSkinningTransform_FinishAccumulate(inout StagedSkinningTransform accumulator) {
	ScalingTransform_FinishAccumulate(accumulator.scalingStage);
	DualQuaternion_FinishAccumulate(accumulator.rotationStage);
}

float3 StagedSkinningTransform_Apply(StagedSkinningTransform transform, float3 p) {
	p = ScalingTransform_Apply(transform.scalingStage, p);
	p = DualQuaternion_Apply(transform.rotationStage, p);
	return p;
}

