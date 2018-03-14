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
}