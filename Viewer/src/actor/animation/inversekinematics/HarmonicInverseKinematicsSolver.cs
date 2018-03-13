using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using static System.Math;

public class HarmonicInverseKinematicsSolver : IInverseKinematicsSolver {
	private const int Iterations = 10;
	private const float MomentOfInertiaCoefficient = 0.3f;
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

	private const int FigureCenterBoneIndex = 1;

	private BonePartialSolution SolveSingleBone(
			RigidBone bone,
			Vector3 worldSource, Vector3 worldTarget, Vector3 figureCenterOverride,
			RigidBoneSystemInputs inputs, DualQuaternion[] boneTransforms) {
		
		var center = bone.Index != FigureCenterBoneIndex ? boneTransforms[bone.Index].Transform(bone.CenterPoint) : figureCenterOverride;
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

	private void ApplyPartialSolution(RigidBone bone, BonePartialSolution partialSolution, DualQuaternion[] boneTransforms, Vector3 figureCenterOverride, RigidBoneSystemInputs inputs, float time) {
		var twistAxis = bone.RotationOrder.TwistAxis;
		var originalRotationQ = inputs.Rotations[bone.Index].AsQuaternion(twistAxis);
		var rotationDelta = QuaternionExtensions.FromRotationVector(time * partialSolution.angularVelocity);
		var newRotationQ = originalRotationQ.Chain(rotationDelta);
		var newRotation = TwistSwing.Decompose(twistAxis, newRotationQ);
			
		inputs.Rotations[bone.Index] = bone.Constraint.Clamp(newRotation);

		if (bone.Index == FigureCenterBoneIndex) {
			var preTotalTransform = boneTransforms[bone.Index];
			var postTotalTransform = bone.GetChainedTransform(inputs, boneTransforms[bone.Parent.Index]);
			var unposedFigureCenterOverride = preTotalTransform.InverseTransform(figureCenterOverride);
			var postFigureCenterOverride = postTotalTransform.Transform(unposedFigureCenterOverride);

			var centerDisplacement = figureCenterOverride - postFigureCenterOverride;
			inputs.RootTranslation += centerDisplacement;
		}
	}
	
	public Vector3[] GetCentersOfMass(DualQuaternion[] totalTransforms) {
		float[] descendantMasses = new float[boneSystem.Bones.Length];
		Vector3[] descendantMassPositions = new Vector3[boneSystem.Bones.Length];
		Vector3[] centersOfMass = new Vector3[boneSystem.Bones.Length];

		foreach (var bone in boneSystem.Bones.Reverse()) {
			float mass = boneAttributes[bone.Index].Mass;
			var position = totalTransforms[bone.Index].Transform(bone.CenterPoint);
			var massPosition = mass * position;

			var totalMass = descendantMasses[bone.Index] + mass;
			var totalMassPosition = descendantMassPositions[bone.Index] + massPosition;

			centersOfMass[bone.Index] = descendantMassPositions[bone.Index] / descendantMasses[bone.Index];

			var parent = bone.Parent;
			if (parent != null) {
				descendantMasses[parent.Index] += totalMass;
				descendantMassPositions[parent.Index] += totalMassPosition;
			}
		}

		return centersOfMass;
	}

	private void CountertransformOffChainBones(DualQuaternion[] preTotalTransforms, Vector3[] preCentersOfMass, RigidBoneSystemInputs inputs, RigidBone[] boneChain) {
		bool[] areOnChain = new bool[boneSystem.Bones.Length];
		areOnChain[0] = true; //root bone is always on the chain
		foreach (var bone in boneChain) {
			areOnChain[bone.Index] = true;
		}
		
		var rootTransform = DualQuaternion.FromTranslation(inputs.RootTranslation);
		DualQuaternion[] postTotalTransforms = new DualQuaternion[preTotalTransforms.Length];

		foreach (var bone in boneSystem.Bones) {
			if (!boneAttributes[bone.Index].IsIkable) {
				//don't even bother to update postTotalTransforms because non-Ikable bones never have Ikable children
				continue;
			}

			var parentPostTotalTransform = bone.Parent != null ? postTotalTransforms[bone.Parent.Index] : rootTransform;
			
			if (!areOnChain[bone.Index]) {
				var preTotalTransform = preTotalTransforms[bone.Index];
				var postTotalTransform = bone.GetChainedTransform(inputs, parentPostTotalTransform);
				
				var preCenterOfRotation = preTotalTransform.Transform(bone.CenterPoint);
				var postCenterOfRotation = postTotalTransform.Transform(bone.CenterPoint);

				var originalWorldRotation = preTotalTransform.Rotation;

				Quaternion centerOfMassRestoringRotation;
				var preChildCenterOfMass = preCentersOfMass[bone.Index];
				if (!float.IsNaN(preChildCenterOfMass.X)) {
					var counterRotationDelta = QuaternionExtensions.RotateBetween(
						Vector3.Normalize(preChildCenterOfMass - preCenterOfRotation),
						Vector3.Normalize(preChildCenterOfMass - postCenterOfRotation));
					centerOfMassRestoringRotation = counterRotationDelta;
				} else {
					//no child center of mass to restore
					centerOfMassRestoringRotation = Quaternion.Identity;
				}
				var worldCounterRotation = originalWorldRotation.Chain(centerOfMassRestoringRotation);
				var counterRotation = worldCounterRotation.Chain(Quaternion.Invert(parentPostTotalTransform.Rotation));

				var originalRotation = bone.GetRotation(inputs);
				float shrinkRatio = boneAttributes[bone.Index].Mass / boneAttributes[0].MassIncludingDescendants;
				var shrunkCounterRotation = Quaternion.Lerp(originalRotation, counterRotation, shrinkRatio);
				bone.SetRotation(inputs, shrunkCounterRotation, true);
			}

			var finalPostTotalTransform = bone.GetChainedTransform(inputs, parentPostTotalTransform);
			postTotalTransforms[bone.Index] = finalPostTotalTransform;
		}
	}

	private void DoIteration(int iteration, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var centersOfMass = GetCentersOfMass(boneTransforms);
		var figureCenterOverride = centersOfMass[0];
		
		var sourcePosition = boneTransforms[problem.SourceBone.Index].Transform(problem.UnposedSourcePosition);

		var bones = GetBoneChain(problem.SourceBone).ToArray();
		//var bones = new RigidBone[] { boneSystem.BonesByName["lForearmBend"], boneSystem.BonesByName["lShldrBend"] };
		
		float totalRate = 0;

		var bonePartialSolutions = new BonePartialSolution[bones.Length];
		for (int i = 0; i < bones.Length; ++i) {
			var partialSolution = SolveSingleBone(bones[i], sourcePosition, problem.TargetPosition, figureCenterOverride, inputs, boneTransforms);

			bonePartialSolutions[i] = partialSolution;
			totalRate += 1 / partialSolution.time;
		}

		var rootTranslationPartialSolution = SolveRootTranslation(sourcePosition, problem.TargetPosition);
		totalRate += 1 / rootTranslationPartialSolution.time;

		float time = 1 / totalRate;

		for (int i = 0; i < bones.Length; ++i) {
			ApplyPartialSolution(bones[i], bonePartialSolutions[i], boneTransforms, figureCenterOverride, inputs, time);
		}
		ApplyPartialSolution(rootTranslationPartialSolution, inputs, time);

		CountertransformOffChainBones(boneTransforms, centersOfMass, inputs, bones);
	}
	
	public void Solve(RigidBoneSystem boneSystem, InverseKinematicsProblem problem, RigidBoneSystemInputs inputs) {
		for (int i = 0; i < Iterations; ++i) {
			DoIteration(i, problem, inputs);
		}
	}
}