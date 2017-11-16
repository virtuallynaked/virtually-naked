using SharpDX;

namespace FlatIk {
	public interface IIkSolver {
		void DoIteration(Bone sourceBone, Vector2 unposedSource, Vector2 target);
	}
}