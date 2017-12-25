using SharpDX;
using System;

public class RotationConstraint {
	private RotationOrder rotationOrder;
	private Vector3 minRotation;
	private Vector3 maxRotation;
	
	public RotationOrder RotationOrder => rotationOrder;

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
	
	public static RotationConstraint InitializeFrom(RotationOrder rotationOrder, ChannelTriplet rotationChannel) {
		ExtractMinMax(rotationChannel, out Vector3 minRotation, out Vector3 maxRotation);
		return new RotationConstraint(rotationOrder, minRotation, maxRotation);
	}

	public RotationConstraint(RotationOrder rotationOrder, Vector3 minRotation, Vector3 maxRotation) {
		this.rotationOrder = rotationOrder;
		this.minRotation = minRotation;
		this.maxRotation = maxRotation;
	}

	private bool IsLocked(int axisIdx) {
		return minRotation[axisIdx] == maxRotation[axisIdx];
	}

	public bool TwistLocked => IsLocked(rotationOrder.primaryAxis);
	public bool SwingLocked => IsLocked(rotationOrder.secondaryAxis) && IsLocked(rotationOrder.tertiaryAxis);
	
	public Vector3 ClampRotation(Vector3 value) {
		float clampedPrimary = MathUtil.Clamp(
			(float) Math.IEEERemainder(value[rotationOrder.primaryAxis], 360),
			minRotation[rotationOrder.primaryAxis], maxRotation[rotationOrder.primaryAxis]);

		float clampedSecondary = (float) Math.IEEERemainder(value[rotationOrder.secondaryAxis], 360);
		float clampedTertiary = (float) Math.IEEERemainder(value[rotationOrder.tertiaryAxis], 360);
		EllipseClamp.ClampToEllipse(
			ref clampedSecondary, ref clampedTertiary,
			minRotation[rotationOrder.secondaryAxis], maxRotation[rotationOrder.secondaryAxis],
			minRotation[rotationOrder.tertiaryAxis], maxRotation[rotationOrder.tertiaryAxis]);

		Vector3 result = default(Vector3);
		result[rotationOrder.primaryAxis] = clampedPrimary;
		result[rotationOrder.secondaryAxis] = clampedSecondary;
		result[rotationOrder.tertiaryAxis] = clampedTertiary;

		return result;
	}

	public Quaternion ClampRotation(Quaternion q) {
		Vector3 rotationAnglesRadians = rotationOrder.ToTwistSwingAngles(q);
		Vector3 rotationAnglesDegrees = MathExtensions.RadiansToDegrees(rotationAnglesRadians);
		Vector3 clampedRotationAnglesDegrees = ClampRotation(rotationAnglesDegrees);
		Vector3 clampedRotationAnglesRadians = MathExtensions.DegreesToRadians(clampedRotationAnglesDegrees);
		Quaternion clampedQ = rotationOrder.FromTwistSwingAngles(clampedRotationAnglesRadians);
		return clampedQ;
	}
}