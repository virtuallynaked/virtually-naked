using SharpDX;

public class DualQuaternionBlender {
	private Quaternion realAccumulator = Quaternion.Zero;
	private Quaternion dualAccumulator = Quaternion.Zero;

	public void Add(float weight, DualQuaternion dq) {
		if (Quaternion.Dot(realAccumulator, dq.Real) < 0) {
			weight *= -1;
		}
		
		realAccumulator += weight * dq.Real;
		dualAccumulator += weight * dq.Dual;
	}

	public DualQuaternion GetResult() {
		float recipLength = 1 / realAccumulator.Length();
		return new DualQuaternion(recipLength * realAccumulator, recipLength * dualAccumulator);
	}
}
