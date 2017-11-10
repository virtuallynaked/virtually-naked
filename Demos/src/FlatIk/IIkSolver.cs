using SharpDX;

namespace FlatIk {
	public interface IIkSolver {
		void DoIteration(Vector2 source, Vector2 target);
	}
}