using MathNet.Numerics.Distributions;
using SharpDX;
using System;
using static System.Math;

public static class RandomUtil {
	public static Swing Swing(Random rnd) {
		var swingTheta = ContinuousUniform.Sample(rnd, -PI, PI);
		var swingMagnitude = ContinuousUniform.Sample(rnd, 0, 1);
		var swingY = swingMagnitude * Cos(swingTheta);
		var swingZ = swingMagnitude * Sin(swingTheta);
		var swing = new Swing((float) swingY, (float) swingZ);
		return swing;
	}

	public static Twist Twist(Random rnd) {
		var twistX = ContinuousUniform.Sample(rnd, -1, 1);
		var twist = new Twist((float) twistX);
		return twist;
	}

	public static TwistSwing TwistSwing(Random rnd) {
		var twist = Twist(rnd);
		var swing = Swing(rnd);
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

	public static Quaternion UnitQuaternion(Random rnd) {
		Quaternion q;
		q.W = (float) Normal.Sample(rnd, 0, 1);
		q.X = (float) Normal.Sample(rnd, 0, 1);
		q.Y = (float) Normal.Sample(rnd, 0, 1);
		q.Z = (float) Normal.Sample(rnd, 0, 1);
		q.Normalize();
		return q;
	}

	public static RigidTransform RigidTransform(Random rnd) {
		var rotation = UnitQuaternion(rnd);
		var translation = Vector3(rnd);
		return new RigidTransform(rotation, translation);
	}
}
