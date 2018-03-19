using SharpDX;

public struct TwistSwingConstraint {
	private readonly TwistConstraint twistConstraint;
	private readonly SwingConstraint swingConstraint;

	public TwistSwingConstraint(TwistConstraint twistConstraint, SwingConstraint swingConstraint) {
		this.twistConstraint = twistConstraint;
		this.swingConstraint = swingConstraint;
	}

	public static TwistSwingConstraint MakeFromRadians(CartesianAxis twistAxis, Vector3 min, Vector3 max) {
		var twistConstraint = TwistConstraint.MakeFromRadians(twistAxis, min, max);
		var swingConstraint = SwingConstraint.MakeFromRadians(twistAxis, min, max);
		return new TwistSwingConstraint(twistConstraint, swingConstraint);
	}

	public TwistConstraint Twist => twistConstraint;
	public SwingConstraint Swing => swingConstraint;

	public TwistSwing Center => new TwistSwing(Twist.Center, Swing.Center);

	public bool Test(TwistSwing twistSwing) {
		return Twist.Test(twistSwing.Twist) && Swing.Test(twistSwing.Swing);
	}

	public TwistSwing Clamp(TwistSwing twistSwing) {
		var clampedTwist = Twist.Clamp(twistSwing.Twist);
		var clampedSwing = Swing.Clamp(twistSwing.Swing);
		return new TwistSwing(clampedTwist, clampedSwing);
	}
}
