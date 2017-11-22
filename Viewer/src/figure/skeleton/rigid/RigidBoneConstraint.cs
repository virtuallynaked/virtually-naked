using SharpDX;

public class RigidBoneConstraint {
	private Vector3 minRotation;
	private Vector3 maxRotation;
	
	private static void ExtractMinMax(Channel channel, int idx, ref Vector3 min, ref Vector3 max) {
		if (!channel.Visible) {
			min[idx] = (float) channel.InitialValue;
			max[idx] = (float) channel.InitialValue;
		} else if (!channel.Clamped) {
			min[idx] = float.NegativeInfinity;
			max[idx] = float.PositiveInfinity;
		} else {
			min[idx] = (float) channel.Min;
			max[idx] = (float) channel.Max;
		}
	}

	private static void ExtractMinMax(ChannelTriplet triplet, out Vector3 min, out Vector3 max) {
		min = Vector3.Zero;
		max = Vector3.Zero;
		ExtractMinMax(triplet.X, 0, ref min, ref max);
		ExtractMinMax(triplet.Y, 1, ref min, ref max);
		ExtractMinMax(triplet.Z, 2, ref min, ref max);
	}
	
	public static RigidBoneConstraint InitializeFrom(Bone source) {
		ExtractMinMax(source.Rotation, out Vector3 minRotation, out Vector3 maxRotation);

		return new RigidBoneConstraint {
			minRotation = minRotation,
			maxRotation = maxRotation,
		};
	}

	public Vector3 ClampRotation(Vector3 value) {
		return Vector3.Clamp(value, minRotation, maxRotation);
	}
}