using System.Collections.Generic;

public interface IInverseKinematicsSolver {
	void Solve(RigidBoneSystem boneSystem, List<InverseKinematicsGoal> goals, RigidBoneSystemInputs inputs);
}
