public interface IInverseKinematicsSolver {
	void Solve(RigidBoneSystem boneSystem, InverseKinematicsGoal goal, RigidBoneSystemInputs inputs);
}
