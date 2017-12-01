public interface IInverseKinematicsSolver {
	void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs);
}
