using SharpDX;
using static MathExtensions;

public struct MassMomentAccumulator {
	private float totalMass;
	private Vector3 totalMassPosition;

	private float totalInertiaXX;
	private float totalInertiaYY;
	private float totalInertiaZZ;
	private float totalInertiaXY;
	private float totalInertiaXZ;
	private float totalInertiaYZ;

	public float Mass => totalMass;
	public Vector3 MassPosition => totalMassPosition;
	public float InertiaXX => totalInertiaXX;
	public float InertiaYY => totalInertiaYY;
	public float InertiaZZ => totalInertiaZZ;
	public float InertiaXY => totalInertiaXY;
	public float InertiaXZ => totalInertiaXZ;
	public float InertiaYZ => totalInertiaYZ;

	public MassMomentAccumulator(
		float mass,
		Vector3 massPosition,
		float inertiaXX,
		float inertiaYY,
		float inertiaZZ,
		float inertiaXY,
		float inertiaXZ,
		float inertiaYZ) {
		totalMass = mass;
		totalMassPosition = massPosition;
		totalInertiaXX = inertiaXX;
		totalInertiaYY = inertiaYY;
		totalInertiaZZ = inertiaZZ;
		totalInertiaXY = inertiaXY;
		totalInertiaXZ = inertiaXZ;
		totalInertiaYZ = inertiaYZ;
	}

	public void Add(float mass, Vector3 position) {
		totalMass += mass;
		totalMassPosition += mass * position;

		totalInertiaXX += mass * (Sqr(position.Y) + Sqr(position.Z));
		totalInertiaYY += mass * (Sqr(position.X) + Sqr(position.Z));
		totalInertiaZZ += mass * (Sqr(position.X) + Sqr(position.Y));

		totalInertiaXY += -mass * position.X * position.Y;
		totalInertiaXZ += -mass * position.X * position.Z;
		totalInertiaYZ += -mass * position.Y * position.Z;
	}

	public void Add(MassMomentAccumulator accumulator) {
		totalMass += accumulator.totalMass;
		totalMassPosition += accumulator.totalMassPosition;
		totalInertiaXX += accumulator.totalInertiaXX;
		totalInertiaYY += accumulator.totalInertiaYY;
		totalInertiaZZ += accumulator.totalInertiaZZ;
		totalInertiaXY += accumulator.totalInertiaXY;
		totalInertiaXZ += accumulator.totalInertiaXZ;
		totalInertiaYZ += accumulator.totalInertiaYZ;
	}

	public Vector3 GetCenterOfMass() {
		return totalMassPosition / totalMass;
	}

	public float GetMomentOfInertia(Vector3 axisOfRotation, Vector3 centerOfRotation) {
		float momentAboutOrigin =
			totalInertiaXX * Sqr(axisOfRotation.X) +
			totalInertiaYY * Sqr(axisOfRotation.Y) +
			totalInertiaZZ * Sqr(axisOfRotation.Z) +
			2 * totalInertiaXY * axisOfRotation.X * axisOfRotation.Y +
			2 * totalInertiaXZ * axisOfRotation.X * axisOfRotation.Z +
			2 * totalInertiaYZ * axisOfRotation.Y * axisOfRotation.Z;

		Vector3 centerOfMass = GetCenterOfMass();
		float momentAboutCenterOfMass = momentAboutOrigin - totalMass * Vector3.Cross(centerOfMass, axisOfRotation).LengthSquared();
		float momentAboutCenterOfRotation = momentAboutCenterOfMass + totalMass * Vector3.Cross(centerOfRotation - centerOfMass, axisOfRotation).LengthSquared();
		return momentAboutCenterOfRotation;
	}

	public MassMomentAccumulator Translate(Vector3 translation) {
		float Mxx = Mass * Sqr(translation.X);
		float Myy = Mass * Sqr(translation.Y);
		float Mzz = Mass * Sqr(translation.Z);
		float Mxy = Mass * translation.X * translation.Y;
		float Mxz = Mass * translation.X * translation.Z;
		float Myz = Mass * translation.Y * translation.Z;

		float MXx = MassPosition.X * translation.X;
		float MYy = MassPosition.Y * translation.Y;
		float MZz = MassPosition.Z * translation.Z;
		
		return new MassMomentAccumulator(
			Mass,
			MassPosition + Mass * translation,
			InertiaXX + Myy + Mzz + 2 * (MYy + MZz),
			InertiaYY + Mxx + Mzz + 2 * (MXx + MZz),
			InertiaZZ + Mxx + Myy + 2 * (MXx + MYy),
			InertiaXY - Mxy - MassPosition.X * translation.Y - MassPosition.Y * translation.X,
			InertiaXZ - Mxz - MassPosition.X * translation.Z - MassPosition.Z * translation.X,
			InertiaYZ - Myz - MassPosition.Y * translation.Z - MassPosition.Z * translation.Y);
	}

	public MassMomentAccumulator Rotate(Quaternion rotation) {
		var r = Matrix3x3.RotationQuaternion(rotation);
		return new MassMomentAccumulator(
			Mass,
			Vector3.Transform(MassPosition, r),
			InertiaXX * Sqr(r.M11) + InertiaYY * Sqr(r.M21) + InertiaZZ * Sqr(r.M31) + 2 * (InertiaXY * r.M11 * r.M21 + InertiaXZ * r.M11 * r.M31 + InertiaYZ * r.M21 * r.M31),
			InertiaXX * Sqr(r.M12) + InertiaYY * Sqr(r.M22) + InertiaZZ * Sqr(r.M32) + 2 * (InertiaXY * r.M12 * r.M22 + InertiaXZ * r.M12 * r.M32 + InertiaYZ * r.M22 * r.M32),
			InertiaXX * Sqr(r.M13) + InertiaYY * Sqr(r.M23) + InertiaZZ * Sqr(r.M33) + 2 * (InertiaXY * r.M13 * r.M23 + InertiaXZ * r.M13 * r.M33 + InertiaYZ * r.M23 * r.M33),
			InertiaXX * r.M11 * r.M12 + InertiaYY * r.M21 * r.M22 + InertiaZZ * r.M31 * r.M32 + InertiaXY*(r.M12*r.M21 + r.M11*r.M22) + InertiaXZ*(r.M12*r.M31 + r.M11*r.M32) + InertiaYZ*(r.M22*r.M31 + r.M21*r.M32),
			InertiaXX * r.M11 * r.M13 + InertiaYY * r.M21 * r.M23 + InertiaZZ * r.M31 * r.M33 + InertiaXY*(r.M13*r.M21 + r.M11*r.M23) + InertiaXZ*(r.M13*r.M31 + r.M11*r.M33) + InertiaYZ*(r.M23*r.M31 + r.M21*r.M33),
			InertiaXX * r.M12 * r.M13 + InertiaYY * r.M22 * r.M23 + InertiaZZ * r.M32 * r.M33 + InertiaXY*(r.M13*r.M22 + r.M12*r.M23) + InertiaXZ*(r.M13*r.M32 + r.M12*r.M33) + InertiaYZ*(r.M23*r.M32 + r.M22*r.M33));
	}
}