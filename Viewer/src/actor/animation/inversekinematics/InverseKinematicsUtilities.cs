using SharpDX;
using static System.Math;
using static MathExtensions;
using static SharpDX.MathUtil;

public static class InverseKinematicsUtilities {
	/**
	 * Calculating a biasing factor that favors angular velocities towards the relaxed rotation.
	 * The biasing factor is 1.5 for velocities directly towards the relaxed rotation, 0.5 for velocities directly away and varies smoothly in between.
	 */
	public static float CalculateRelaxationBias(Quaternion relaxedRotation, Quaternion currentRotation, Vector3 angularVelocity) {
		var currentToRelaxedQ = Quaternion.Invert(currentRotation).Chain(relaxedRotation);
		
		//The code below is equivalent to this, but faster:
		// var currentToRelaxedV = currentToRelaxed.ToRotationVector();
		// float dot = Vector3.Dot(Vector3.Normalize(angularVelocity), Vector3.Normalize(currentToRelaxedV));
		float unnormalizedDot =
			angularVelocity.X * currentToRelaxedQ.X +
			angularVelocity.Y * currentToRelaxedQ.Y +
			angularVelocity.Z * currentToRelaxedQ.Z;
		float currentToRelaxedLength = Sign(currentToRelaxedQ.W) * (float) Sqrt(Sqr(currentToRelaxedQ.X) + Sqr(currentToRelaxedQ.Y) + Sqr(currentToRelaxedQ.Z));
		float angularVelocityLength = angularVelocity.Length();
		float lengthProduct = currentToRelaxedLength * angularVelocityLength;
		if (IsZero(lengthProduct)) {
			return 1;
		}
		float dot = unnormalizedDot / lengthProduct;
		
		float relaxationBias = 1 + dot / 2;
		return relaxationBias;
	}
}