using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;

[TestClass]
public class InverseKinematicsUtilitiesTest {
	private const float Acc = 1e-4f;
	
	[TestMethod]
	public void TestCalculateRelaxationBiasDirectionality() {
		var rnd = new Random(1);
		var relaxedRotation = RandomUtil.UnitQuaternion(rnd);
		var currentRotation = RandomUtil.UnitQuaternion(rnd);

		var towardsRelaxedVelocity = Quaternion.Invert(currentRotation).Chain(relaxedRotation).ToRotationVector();
		Assert.AreEqual(1.5f, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, towardsRelaxedVelocity), Acc);
		
		var awayFromRelaxedVelocity = -towardsRelaxedVelocity;
		Assert.AreEqual(0.5f, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, awayFromRelaxedVelocity), Acc);
		
		var orthogonalToRelaxedVelocity = Vector3.Cross(Vector3.UnitX, towardsRelaxedVelocity);
		Assert.AreEqual(1f, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, orthogonalToRelaxedVelocity), Acc);
	}

	[TestMethod]
	public void TestCalculateRelaxationBiasScaleInvariance() {
		var rnd = new Random(0);
		var relaxedRotation = RandomUtil.UnitQuaternion(rnd);
		var currentRotation = RandomUtil.UnitQuaternion(rnd);

		var velocity = RandomUtil.Vector3(rnd);

		var expected = InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, velocity);
		Assert.AreEqual(expected, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, 2 * velocity), Acc);
		Assert.AreEqual(expected, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, 0.5f * velocity), Acc);
	}

	[TestMethod]
	public void TestCalculateRelaxationBiasQuaternionInvariance() {
		var rnd = new Random(0);
		var relaxedRotation = RandomUtil.UnitQuaternion(rnd);
		var currentRotation = RandomUtil.UnitQuaternion(rnd);

		var velocity = RandomUtil.Vector3(rnd);

		var expected = InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, velocity);
		Assert.AreEqual(expected, InverseKinematicsUtilities.CalculateRelaxationBias(-relaxedRotation, currentRotation, velocity), Acc);
		Assert.AreEqual(expected, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, -currentRotation, velocity), Acc);
		Assert.AreEqual(expected, InverseKinematicsUtilities.CalculateRelaxationBias(-relaxedRotation, -currentRotation, velocity), Acc);
	}

	[TestMethod]
	public void TestCalculateRelaxationBiasAtZeroVelocity() {
		var rnd = new Random(0);
		var relaxedRotation = RandomUtil.UnitQuaternion(rnd);
		var currentRotation = RandomUtil.UnitQuaternion(rnd);

		Assert.AreEqual(1, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, Vector3.Zero));
	}

	[TestMethod]
	public void TestCalculateRelaxationBiasAtFullyRelaxed() {
		var rnd = new Random(0);
		var relaxedRotation = RandomUtil.UnitQuaternion(rnd);
		var currentRotation = relaxedRotation;

		Vector3 velocity = RandomUtil.Vector3(rnd);

		Assert.AreEqual(1, InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, currentRotation, velocity));
	}
}
