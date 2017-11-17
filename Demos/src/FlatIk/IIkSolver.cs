using SharpDX;

namespace FlatIk {
	public interface IIkSolver {
		void DoIteration(SkeletonInputs inputs, Bone sourceBone, Vector2 unposedSource, Vector2 target);
	}
}