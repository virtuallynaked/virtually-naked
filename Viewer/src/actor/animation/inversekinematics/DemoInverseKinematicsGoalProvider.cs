using SharpDX;
using System.Collections.Generic;

public class DemoInverseKinematicsGoalProvider : IInverseKinematicsGoalProvider {
	private readonly RigidBoneSystem boneSystem;

	public DemoInverseKinematicsGoalProvider(RigidBoneSystem boneSystem) {
		this.boneSystem = boneSystem;
	}

	private InverseKinematicsGoal MakeMoveHandDownGoal(RigidBoneSystemInputs inputs) {
		var forearmBone = boneSystem.BonesByName["lForearmBend"];
		var handBone = boneSystem.BonesByName["lHand"];
		return new InverseKinematicsGoal(
			forearmBone,
			handBone.CenterPoint,
			forearmBone.CenterPoint + Vector3.Down * Vector3.Distance(handBone.CenterPoint, forearmBone.CenterPoint));
	}

	private InverseKinematicsGoal MakeKeepFootInPlaceGoal(RigidBoneSystemInputs inputs) {
		return new InverseKinematicsGoal(
			boneSystem.BonesByName["lShin"],
			boneSystem.BonesByName["lFoot"].CenterPoint,
			boneSystem.BonesByName["lFoot"].GetChainedTransform(inputs).Transform(boneSystem.BonesByName["lFoot"].CenterPoint));
	}

	public List<InverseKinematicsGoal> GetGoals(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		return new List<InverseKinematicsGoal> {
			MakeMoveHandDownGoal(inputs),
			//MakeKeepFootInPlaceGoal(inputs)
		};
	}
}
