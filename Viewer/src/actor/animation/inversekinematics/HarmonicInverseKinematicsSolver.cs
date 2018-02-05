using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using static System.Math;
using static SharpDX.MathUtil;
using static MathExtensions;

public class HarmonicInverseKinematicsSolver : IInverseKinematicsSolver {
	private const int Iterations = 10;

	private readonly BoneAttributes[] boneAttributes;

	public HarmonicInverseKinematicsSolver(BoneAttributes[] boneAttributes) {
		this.boneAttributes = boneAttributes;
	}

	private IEnumerable<RigidBone> GetBoneChain(RigidBone sourceBone) {
		for (var bone = sourceBone; bone != null; bone = bone.Parent) {
			if (bone.Parent == null) {
				//omit root bone
				continue;
			}

			yield return bone;
		}
	}
	
	private struct BonePartialSolution {
		public Vector3 angularVelocity;
		public float time;
	}

	private static float BetterAngle(Quaternion q) {
		double lengthSquared = Sqr((double) q.X) + Sqr((double) q.Y) + Sqr((double) q.Z);
		double angle = 2 * Math.Asin(Sqrt(lengthSquared));
		return (float) angle;
	}

	private BonePartialSolution SolveSingleBone(
			RigidBone bone,
			Vector3 worldSource, Vector3 worldTarget,
			RigidBoneSystemInputs inputs, DualQuaternion[] boneTransforms) {
			
		var center = boneTransforms[bone.Index].Transform(bone.CenterPoint);
		var parentTotalRotation = bone.Parent != null ? boneTransforms[bone.Parent.Index].Rotation : Quaternion.Identity;
		var boneToWorldSpaceRotation = bone.OrientationSpace.Orientation.Chain(parentTotalRotation);
		var worldToBoneSpaceRotation = Quaternion.Invert(boneToWorldSpaceRotation);
		var boneSpaceSource = Vector3.Transform(worldSource - center, worldToBoneSpaceRotation);
		var boneSpaceTarget = Vector3.Transform(worldTarget - center, worldToBoneSpaceRotation);
		
		var targetOnSphere = boneSpaceSource.Length() * Vector3.Normalize(boneSpaceTarget);
		
		var rotation = QuaternionExtensions.RotateBetween(
			Vector3.Normalize(boneSpaceSource),
			Vector3.Normalize(boneSpaceTarget));

		var rotatedSource = Vector3.Transform(boneSpaceSource, rotation);
		
		var radius = boneSpaceSource.Length();
		var distance = BetterAngle(rotation) * radius;
		
		var force = boneSpaceTarget - boneSpaceSource;
		var torque = Vector3.Cross(boneSpaceSource, force);
		float mass = boneAttributes[bone.Index].Mass;
		float momentOfInertia = (float) Pow(mass, 5/3f);
		var angularVelocity = torque / momentOfInertia;
		var linearVelocity = Vector3.Cross(angularVelocity, boneSpaceSource);
		
		float time = distance == 0 ? 0 : distance / linearVelocity.Length();

		return new BonePartialSolution {
			angularVelocity = angularVelocity,
			time = time
		};
	}

	private void DoIteration(int iteration, RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.UnposedSourcePosition);

		var bones = GetBoneChain(problem.SourceBone).ToArray();
		//var bones = new RigidBone[] { boneSystem.BonesByName["lForearmBend"], boneSystem.BonesByName["lShldrBend"] };
		
		float totalRate = 0;
		var bonePartialSolutions = new BonePartialSolution[bones.Length];
		for (int i = 0; i < bones.Length; ++i) {
			bonePartialSolutions[i] = SolveSingleBone(bones[i], sourcePosition, problem.TargetPosition, inputs, boneTransforms);
			totalRate += 1 / bonePartialSolutions[i].time;
		}
		
		float time = 1 / totalRate;

		for (int i = 0; i < bones.Length; ++i) {
			var bone = bones[i];
			var partialSolution = bonePartialSolutions[i];

			var twistAxis = bone.RotationOrder.TwistAxis;
			var originalRotationQ = inputs.Rotations[bone.Index].AsQuaternion(twistAxis);
			var rotationDelta = AngularVelocityToQ(partialSolution.angularVelocity, time);
			var newRotationQ = originalRotationQ.Chain(rotationDelta);
			var newRotation = TwistSwing.Decompose(twistAxis, newRotationQ);
			
			inputs.Rotations[bone.Index] = bone.Constraint.Clamp(newRotation);
		}
	}

	private Quaternion AngularVelocityToQ(Vector3 angularVelocity, float time) {
		Quaternion logQ = new Quaternion(angularVelocity * time / 2, 0);
		return Quaternion.Exponential(logQ);
	}

	public void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		for (int i = 0; i < Iterations; ++i) {
			DoIteration(i, boneSystem, problem, inputs);
		}
	}
}