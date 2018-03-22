using System.Collections.Generic;

public interface IInverseKinematicsGoalProvider {
	List<InverseKinematicsGoal> GetGoals(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos);
}
