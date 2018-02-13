using SharpDX;
using System.Diagnostics;
using static MathExtensions;
using static System.Math;

public struct SwingConstraint {
	//constraints are expressed as Sin[angle/2]
	public float MinY { get; }
	public float MaxY { get; }
	public float MinZ { get; }
	public float MaxZ { get; }

	public bool IsLocked => MinY == MaxY && MinZ == MaxZ;

	public SwingConstraint(float minY, float maxY, float minZ, float maxZ) {
		Debug.Assert(minY <= 0);
		Debug.Assert(maxY >= 0);
		Debug.Assert(minZ <= 0);
		Debug.Assert(maxZ >= 0);

		MinY = minY;
		MaxY = maxY;
		MinZ = minZ;
		MaxZ = maxZ;
	}

	public static float SinHalfAngleFromRadians(float radians) {
		return (float) Sin(MathUtil.Clamp(radians, -MathUtil.Pi, +MathUtil.Pi) / 2);
	}

	public static SwingConstraint MakeSymmetric(float limitY, float limitZ) {
		return new SwingConstraint(-limitY, +limitY, -limitZ, +limitZ);
	}

	public static SwingConstraint MakeSymmetric(float limit) {
		return MakeSymmetric(limit, limit);
	}

	public static SwingConstraint MakeFromRadians(float minY, float maxY, float minZ, float maxZ) {
		return new SwingConstraint(
			SinHalfAngleFromRadians(minY),
			SinHalfAngleFromRadians(maxY),
			SinHalfAngleFromRadians(minZ),
			SinHalfAngleFromRadians(maxZ));
	}

	public static SwingConstraint MakeFromRadians(CartesianAxis twistAxis, Vector3 min, Vector3 max) {
		return MakeFromRadians(
			min[((int) twistAxis + 1) % 3], max[((int) twistAxis + 1) % 3],
			min[((int) twistAxis + 2) % 3], max[((int) twistAxis + 2) % 3]);
	}
	
	public static SwingConstraint MakeSymmetricFromRadians(float limitY, float limitZ) {
		return MakeSymmetric(SinHalfAngleFromRadians(limitY), SinHalfAngleFromRadians(limitZ));
	}

	public static SwingConstraint MakeSymmetricFromRadians(float limit) {
		return MakeSymmetric(SinHalfAngleFromRadians(limit));
	}
	
	public bool Test(Swing swing) {
		float limitY = swing.Y < 0 ? MinY : MaxY;
		float limitZ = swing.Z < 0 ? MinZ : MaxZ;
		float rSqr = Sqr(swing.Y / limitY) + Sqr(swing.Z / limitZ);

		return rSqr <= 1;
	}
	
	public Swing Clamp(Swing swing) {
		float y = swing.Y;
		float z = swing.Z;
		EllipseClamp.ClampToEllipse(ref y, ref z, MinY, MaxY, MinZ, MaxZ);
		return new Swing(y, z);
	}
}