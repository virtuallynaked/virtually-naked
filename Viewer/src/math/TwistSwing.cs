﻿using SharpDX;
using static System.Math;
using static MathExtensions;

public struct TwistSwing {
	private readonly Twist twist;
	private readonly Swing swing;

	public TwistSwing(Twist twist, Swing swing) {
		this.twist = twist;
		this.swing = swing;
	}

	public static TwistSwing MakeFromCoordinates(float x, float y, float z) {
		return new TwistSwing(new Twist(x), new Swing(y, z));
	}

	public Twist Twist => twist;
	public Swing Swing => swing;

	public static readonly TwistSwing Zero = new TwistSwing(Twist.Zero, Swing.Zero);

	override public string ToString() {
		return string.Format("TwistSwing[{0}, {1}, {2}]", Twist.X, Swing.Y, Swing.Z);
	}

	public Quaternion AsQuaternion(CartesianAxis twistAxis) {
		float twistX = twist.X;
		float twistW = twist.W;

		float swingY = swing.Y;
		float swingZ = swing.Z;
		float swingW = swing.W;

		Quaternion q = default(Quaternion);
		q[(int) twistAxis] = swingW * twistX;
		q[((int) twistAxis + 1) % 3] = twistW * swingY + twistX * swingZ;
		q[((int) twistAxis + 2) % 3] = twistW * swingZ - twistX * swingY;
		q.W = swingW * twistW;
		return q;
	}

	public static TwistSwing Decompose(CartesianAxis twistAxis, Quaternion q) {
		DebugUtilities.AssertIsUnit(q);

		float w = q.W;
		float x = q[(int) twistAxis];
		float y = q[((int) twistAxis + 1) % 3];
		float z = q[((int) twistAxis + 2) % 3];

		float swingW = (float) Sqrt(Sqr(w) + Sqr(x));
		
		float twistW, twistZ;
		if (swingW != 0) {
			twistW = w / swingW;
			twistZ = x / swingW;
		} else {
			//if swingW is 0, then there isn't a unique decomposition, so I'll arbitrarily assume no twist
			twistW = 1;
			twistZ = 0;
		}
		
		float swingY = twistW * y - twistZ * z;
		float swingZ = twistW * z + twistZ * y;

		var twist = new Twist(Sign(twistW) * twistZ);
		var swing = new Swing(swingY, swingZ);
		return new TwistSwing(twist, swing);
	}

	public static TwistSwing operator+(TwistSwing ts1, TwistSwing ts2) {
		return new TwistSwing(ts1.twist + ts2.twist, ts1.swing + ts2.swing);
	}

	public static TwistSwing operator-(TwistSwing ts1, TwistSwing ts2) {
		return new TwistSwing(ts1.twist - ts2.twist, ts1.swing - ts2.swing);
	}
}