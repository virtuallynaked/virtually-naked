using SharpDX;
using static MathExtensions;

public struct MassMoment {
	private float mass;
	private Vector3 massPosition;

	private float inertiaXX;
	private float inertiaYY;
	private float inertiaZZ;
	private float inertiaXY;
	private float inertiaXZ;
	private float inertiaYZ;
	
	public MassMoment(
		float mass,
		Vector3 massPosition,
		float inertiaXX,
		float inertiaYY,
		float inertiaZZ,
		float inertiaXY,
		float inertiaXZ,
		float inertiaYZ) {
		this.mass = mass;
		this.massPosition = massPosition;
		this.inertiaXX = inertiaXX;
		this.inertiaYY = inertiaYY;
		this.inertiaZZ = inertiaZZ;
		this.inertiaXY = inertiaXY;
		this.inertiaXZ = inertiaXZ;
		this.inertiaYZ = inertiaYZ;
	}

	public float Mass => mass;
	public Vector3 MassPosition => massPosition;
	public float InertiaXX => inertiaXX;
	public float InertiaYY => inertiaYY;
	public float InertiaZZ => inertiaZZ;
	public float InertiaXY => inertiaXY;
	public float InertiaXZ => inertiaXZ;
	public float InertiaYZ => inertiaYZ;

	public override string ToString() {
		return string.Format("MassMoment[Mass = {0}, CenterOfMass = {1}, Inertia = {{{2}, {3}, {4}, {5}, {6}, {7}}}]",
			Mass,
			GetCenterOfMass().FormatForMathematica(),
			InertiaXX, InertiaYY, InertiaZZ, InertiaXY, InertiaXZ, inertiaYZ);
	}

	public void AddInplace(float mass, Vector3 position) {
		this.mass += mass;
		massPosition += mass * position;

		inertiaXX += mass * (Sqr(position.Y) + Sqr(position.Z));
		inertiaYY += mass * (Sqr(position.X) + Sqr(position.Z));
		inertiaZZ += mass * (Sqr(position.X) + Sqr(position.Y));

		inertiaXY += -mass * position.X * position.Y;
		inertiaXZ += -mass * position.X * position.Z;
		inertiaYZ += -mass * position.Y * position.Z;
	}


	public void AddInplace(MassMoment accumulator) {
		mass += accumulator.mass;
		massPosition += accumulator.massPosition;
		inertiaXX += accumulator.inertiaXX;
		inertiaYY += accumulator.inertiaYY;
		inertiaZZ += accumulator.inertiaZZ;
		inertiaXY += accumulator.inertiaXY;
		inertiaXZ += accumulator.inertiaXZ;
		inertiaYZ += accumulator.inertiaYZ;
	}

	public Vector3 GetCenterOfMass() {
		if (mass == 0) {
			return Vector3.Zero;
		} else {
			return massPosition / mass;
		}
	}

	public float GetMomentOfInertia(Vector3 axisOfRotation, Vector3 centerOfRotation) {
		float momentAboutOrigin =
			inertiaXX * Sqr(axisOfRotation.X) +
			inertiaYY * Sqr(axisOfRotation.Y) +
			inertiaZZ * Sqr(axisOfRotation.Z) +
			2 * inertiaXY * axisOfRotation.X * axisOfRotation.Y +
			2 * inertiaXZ * axisOfRotation.X * axisOfRotation.Z +
			2 * inertiaYZ * axisOfRotation.Y * axisOfRotation.Z;

		Vector3 centerOfMass = GetCenterOfMass();
		float momentAboutCenterOfMass = momentAboutOrigin - mass * Vector3.Cross(centerOfMass, axisOfRotation).LengthSquared();
		float momentAboutCenterOfRotation = momentAboutCenterOfMass + mass * Vector3.Cross(centerOfRotation - centerOfMass, axisOfRotation).LengthSquared();
		return momentAboutCenterOfRotation;
	}

	public MassMoment Translate(Vector3 translation) {
		float Mxx = Mass * Sqr(translation.X);
		float Myy = Mass * Sqr(translation.Y);
		float Mzz = Mass * Sqr(translation.Z);
		float Mxy = Mass * translation.X * translation.Y;
		float Mxz = Mass * translation.X * translation.Z;
		float Myz = Mass * translation.Y * translation.Z;

		float MXx = MassPosition.X * translation.X;
		float MYy = MassPosition.Y * translation.Y;
		float MZz = MassPosition.Z * translation.Z;
		
		return new MassMoment(
			Mass,
			MassPosition + Mass * translation,
			InertiaXX + Myy + Mzz + 2 * (MYy + MZz),
			InertiaYY + Mxx + Mzz + 2 * (MXx + MZz),
			InertiaZZ + Mxx + Myy + 2 * (MXx + MYy),
			InertiaXY - Mxy - MassPosition.X * translation.Y - MassPosition.Y * translation.X,
			InertiaXZ - Mxz - MassPosition.X * translation.Z - MassPosition.Z * translation.X,
			InertiaYZ - Myz - MassPosition.Y * translation.Z - MassPosition.Z * translation.Y);
	}

	public MassMoment Rotate(Quaternion rotation) {
		var r = Matrix3x3.RotationQuaternion(rotation);
		return new MassMoment(
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