using SharpDX;

public class OrientationSpace {
	private readonly Quaternion orientation;
	private readonly Quaternion inverseOrientation;

	public OrientationSpace(Quaternion orientation) {
		this.orientation = orientation;

		this.inverseOrientation = orientation;
		inverseOrientation.Invert();
	}

	public Quaternion Orientation => orientation;
	public Quaternion OrientationInverse => inverseOrientation;

	public Quaternion TransformFromOrientedSpace(Quaternion orientedSpaceRotation) {
		Quaternion objectSpaceRotation = inverseOrientation.Chain(orientedSpaceRotation).Chain(orientation);
		return objectSpaceRotation;
	}

	public Quaternion TransformToOrientedSpace(Quaternion objectSpaceRotation) {
		Quaternion orientatedSpaceRotation = orientation.Chain(objectSpaceRotation).Chain(inverseOrientation);
		return orientatedSpaceRotation;
	}

	public Matrix3x3 TransformToOrientedSpace(Matrix3x3 objectSpaceScaling) {
		return Matrix3x3.RotationQuaternion(orientation) * objectSpaceScaling * Matrix3x3.RotationQuaternion(inverseOrientation);
	}

	public void DecomposeIntoTwistThenSwing(Vector3 axis, Quaternion objectSpaceRotation, out Quaternion twist, out Quaternion swing) {
		Quaternion orientatedRotation = TransformToOrientedSpace(objectSpaceRotation);

		orientatedRotation.DecomposeIntoTwistThenSwing(axis, out Quaternion orientedTwist, out Quaternion orientedSwing);
		twist = TransformFromOrientedSpace(orientedTwist);
		swing = TransformFromOrientedSpace(orientedSwing);
	}
}
