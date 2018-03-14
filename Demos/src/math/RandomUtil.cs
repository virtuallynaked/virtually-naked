using MathNet.Numerics.Distributions;
using SharpDX;
using System;
using static System.Math;

public static class RandomUtil {
	public static TwistSwing TwistSwing(Random rnd) {
		var twistX = ContinuousUniform.Sample(rnd, -1, 1);
		var twist = new Twist((float) twistX);

		var swingTheta = ContinuousUniform.Sample(rnd, -PI, PI);
		var swingMagnitude = ContinuousUniform.Sample(rnd, 0, 1);
		var swingY = swingMagnitude * Cos(swingTheta);
		var swingZ = swingMagnitude * Sin(swingTheta);
		var swing = new Swing((float) swingY, (float) swingZ);

		return new TwistSwing(twist, swing);
	}

	public static Vector3 Vector3(Random rnd) {
		Vector3 v;
		v.X = (float) Normal.Sample(rnd, 0, 1);
		v.Y = (float) Normal.Sample(rnd, 0, 1);
		v.Z = (float) Normal.Sample(rnd, 0, 1);
		return v;
	}

	public static float PositiveFloat(Random rnd) {
		return (float) Gamma.Sample(rnd, 1, 1);
	}
	
	public static Vector3 UnitVector3(Random rnd) {
		Vector3 v = Vector3(rnd);
		v.Normalize();
		return v;
	}
}