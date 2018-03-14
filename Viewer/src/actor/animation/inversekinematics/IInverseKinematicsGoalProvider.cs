public interface IInverseKinematicsGoalProvider {
	InverseKinematicsGoal GetGoal(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos);
}