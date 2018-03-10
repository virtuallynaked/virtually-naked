using System.Collections.Generic;
using System.Linq;
using SharpDX;
using static System.Math;

public class HarmonicInverseKinematicsSolver : IInverseKinematicsSolver {
	private const int Iterations = 10;
	private const float MomentOfInertiaCoefficient = 0.5f;
	private const float MetersPerCentimeter = 0.01f; 

	private readonly RigidBoneSystem boneSystem;
	private readonly BoneAttributes[] boneAttributes;

	public HarmonicInverseKinematicsSolver(RigidBoneSystem boneSystem, BoneAttributes[] boneAttributes) {
		this.boneSystem = boneSystem;
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
	
	private struct RootTranslationPartialSolution {
		public Vector3 linearVelocity;
		public float time;
	}

	private RootTranslationPartialSolution SolveRootTranslation(
			Vector3 worldSource, Vector3 worldTarget) {
		var force = (worldTarget - worldSource);
		float mass = boneAttributes[0].MassIncludingDescendants;
		var linearVelocity = force / mass;
		var time = mass;

		return new RootTranslationPartialSolution {
			linearVelocity = linearVelocity,
			time = time,
		};
	}

	private void ApplyPartialSolution(RootTranslationPartialSolution partialSolution, RigidBoneSystemInputs inputs, float time) {
		inputs.RootTranslation += time * partialSolution.linearVelocity;
	}

	private struct BonePartialSolution {
		public Vector3 angularVelocity;
		public float time;
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
		
		var force = boneSpaceTarget - boneSpaceSource;
		var torque = Vector3.Cross(boneSpaceSource * MetersPerCentimeter, force);
		float mass = boneAttributes[bone.Index].MassIncludingDescendants;
		float momentOfInertia = MomentOfInertiaCoefficient * (float) Pow(mass, 5/3f);
		var angularVelocity = torque / momentOfInertia;
		var linearVelocity = Vector3.Cross(angularVelocity, boneSpaceSource);

		var rotation = QuaternionExtensions.RotateBetween(
			Vector3.Normalize(boneSpaceSource),
			Vector3.Normalize(boneSpaceTarget));
		var radius = boneSpaceSource.Length();
		var distance = rotation.AccurateAngle() * radius;

		float time = distance == 0 ? 0 : distance / linearVelocity.Length();

		return new BonePartialSolution {
			angularVelocity = angularVelocity,
			time = time
		};
	}

	private void ApplyPartialSolution(RigidBone bone, BonePartialSolution partialSolution, RigidBoneSystemInputs inputs, float time) {
		var twistAxis = bone.RotationOrder.TwistAxis;
		var originalRotationQ = inputs.Rotations[bone.Index].AsQuaternion(twistAxis);
		var rotationDelta = QuaternionExtensions.FromRotationVector(time * partialSolution.angularVelocity);
		var newRotationQ = originalRotationQ.Chain(rotationDelta);
		var newRotation = TwistSwing.Decompose(twistAxis, newRotationQ);
			
		inputs.Rotations[bone.Index] = bone.Constraint.Clamp(newRotation);
	}

	private void DoIteration(int iteration, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.UnposedSourcePosition);

		var bones = GetBoneChain(problem.SourceBone).ToArray();
		//var bones = new RigidBone[] { boneSystem.BonesByName["lForearmBend"], boneSystem.BonesByName["lShldrBend"] };
		
		float totalRate = 0;

		var bonePartialSolutions = new BonePartialSolution[bones.Length];
		for (int i = 0; i < bones.Length; ++i) {
			var partialSolution = SolveSingleBone(bones[i], sourcePosition, problem.TargetPosition, inputs, boneTransforms);

			bonePartialSolutions[i] = partialSolution;
			totalRate += 1 / partialSolution.time;
		}

		var rootTranslationPartialSolution = SolveRootTranslation(sourcePosition, problem.TargetPosition);
		totalRate += 1 / rootTranslationPartialSolution.time;

		float time = 1 / totalRate;

		for (int i = 0; i < bones.Length; ++i) {
			ApplyPartialSolution(bones[i],  bonePartialSolutions[i], inputs, time);
		}
		ApplyPartialSolution(rootTranslationPartialSolution, inputs, time);
	}
	
	public void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		for (int i = 0; i < Iterations; ++i) {
			DoIteration(i, problem, inputs);
		}
	}
}