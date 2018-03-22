using ProtoBuf;
using SharpDX;

[ProtoContract(UseProtoMembersOnly = true)]
public struct RigidTransform {
	[ProtoMember(1)]
	private readonly Quaternion rotation;

	[ProtoMember(2)]
	private readonly Vector3 translation;

	public Quaternion Rotation => rotation;
	public Vector3 Translation => translation;
	
	public RigidTransform(Quaternion rotation, Vector3 translation) {
		this.rotation = rotation;
		this.translation = translation;
	}

	public static readonly RigidTransform Identity = new RigidTransform(Quaternion.Identity, Vector3.Zero);
	
	public static RigidTransform FromMatrix(Matrix matrix) {
		Vector3 translation = new Vector3(matrix.M41, matrix.M42, matrix.M43);

		var rotationMatrix = new Matrix3x3(
			matrix.M11, matrix.M12, matrix.M13,
			matrix.M21, matrix.M22, matrix.M23,
			matrix.M31, matrix.M32, matrix.M33);
		rotationMatrix.Orthonormalize();
		
		Quaternion.RotationMatrix(ref rotationMatrix, out Quaternion rotation);
		
		return FromRotationTranslation(rotation, translation);
	}

	public static RigidTransform FromRotation(Quaternion rotation) {
		return new RigidTransform(rotation, Vector3.Zero);
	}

	public static RigidTransform FromRotation(Quaternion rotation, Vector3 center) {
		return new RigidTransform(
			rotation,
			center - Vector3.Transform(center, rotation));
	}

	public static RigidTransform FromRotationTranslation(Quaternion rotation, Vector3 translation) {
		return new RigidTransform(rotation, translation);
	}

	public static RigidTransform FromTranslation(Vector3 translation) {
		return new RigidTransform(Quaternion.Identity, translation);
	}

	public Vector3 Transform(Vector3 v) {
		return Vector3.Transform(v, Rotation) + Translation;
	}
	
	public Vector3 InverseTransform(Vector3 v) {
		return Vector3.Transform(v - Translation, Quaternion.Invert(Rotation));
	}
	
	public RigidTransform Chain(RigidTransform transform2) {
		return new RigidTransform(
			transform2.Rotation * Rotation,
			Vector3.Transform(translation, transform2.Rotation) + transform2.Translation);
	}
	
	public Matrix ToMatrix() {
		return Matrix.RotationQuaternion(Rotation) * Matrix.Translation(Translation);
	}

	public RigidTransform Invert() {
		var inverseRotation = Quaternion.Invert(Rotation);
		var inverseTranslation = Vector3.Transform(-Translation, inverseRotation);
		return FromRotationTranslation(inverseRotation, inverseTranslation);
	}

	public override string ToString() {
		return $"RigidTransform[Rotation={Rotation.FormatForMathematica()}, Translation={Translation.FormatForMathematica()}]";
	}
}
