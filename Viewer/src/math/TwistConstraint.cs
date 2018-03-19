using SharpDX;
using System.Diagnostics;
using static System.Math;

public struct TwistConstraint {
	public float MinX { get; }
	public float MaxX { get; }

	public bool IsLocked => MinX == MaxX;

	//constraints are expressed as Sin[angle/2]
	public TwistConstraint(float minX, float maxX) {
		Debug.Assert(minX <= maxX);

		MinX = minX;
		MaxX = maxX;
	}

	public static TwistConstraint MakeFromRadians(float minXRadians, float maxXRadians) {
		float minX = (float) Sin(Max(minXRadians, -PI) / 2);
		float maxX = (float) Sin(Min(maxXRadians, +PI) / 2);
		return new TwistConstraint(minX, maxX);
	}

	public static TwistConstraint MakeFromRadians(CartesianAxis twistAxis, Vector3 min, Vector3 max) {
		return MakeFromRadians(
			min[(int) twistAxis],
			max[(int) twistAxis]
		);
	}

	public Twist Center => new Twist((MinX + MaxX) / 2);

	public bool Test(Twist twist) {
		return twist.X >= MinX && twist.X <= MaxX;
	}

	public Twist Clamp(Twist twist) {
		float clampedX = MathUtil.Clamp(twist.X, MinX, MaxX);
		return new Twist(clampedX);
	}
}