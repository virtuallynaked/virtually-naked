using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using static System.Math;
using static MathExtensions;

public class HarmonicInverseKinematicsSolver : IInverseKinematicsSolver {
	private const int Iterations = 10;

	private readonly RigidBoneSystem boneSystem;
	private readonly BoneAttributes[] boneAttributes;
	private readonly bool[] areOrientable;

	public HarmonicInverseKinematicsSolver(RigidBoneSystem boneSystem, BoneAttributes[] boneAttributes) {
		this.boneSystem = boneSystem;
		this.boneAttributes = boneAttributes;
		areOrientable = MakeAreOrientable(boneSystem);
	}

	private static bool[] MakeAreOrientable(RigidBoneSystem boneSystem) {
		bool[] areOrientable = new bool[boneSystem.Bones.Length];
		areOrientable[boneSystem.BonesByName["lHand"].Index] = true;
		areOrientable[boneSystem.BonesByName["rHand"].Index] = true;
		areOrientable[boneSystem.BonesByName["lFoot"].Index] = true;
		areOrientable[boneSystem.BonesByName["rFoot"].Index] = true;
		return areOrientable;
	}

	private IEnumerable<RigidBone> GetBoneChain(RigidBone sourceBone, bool hasOrientationGoal) {
		for (var bone = sourceBone; bone != null; bone = bone.Parent) {
			if (bone.Parent == null) {
				//omit root bone
				continue;
			}

			if (areOrientable[bone.Index] && hasOrientationGoal) {
				//omit orientable bones if there's a orientation goal since rotation for those bone is already set
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
			Vector3 worldSource, Vector3 worldTarget, MassMoment[] massMoments, Vector3 figureCenterOverride,
			RigidBoneSystemInputs inputs, RigidTransform[] boneTransforms) {
		
		var center = bone.Index != FigureCenterBoneIndex ? boneTransforms[bone.Index].Transform(bone.CenterPoint) : figureCenterOverride;
		var parentTotalRotation = bone.Parent != null ? boneTransforms[bone.Parent.Index].Rotation : Quaternion.Identity;
		var boneToWorldSpaceRotation = bone.OrientationSpace.Orientation.Chain(parentTotalRotation);
		var worldToBoneSpaceRotation = Quaternion.Invert(boneToWorldSpaceRotation);
		var boneSpaceSource = Vector3.Transform(worldSource - center, worldToBoneSpaceRotation);
		var boneSpaceTarget = Vector3.Transform(worldTarget - center, worldToBoneSpaceRotation);
		
		var force = boneSpaceTarget - boneSpaceSource;
		var torque = Vector3.Cross(boneSpaceSource, force);
		float mass = boneAttributes[bone.Index].MassIncludingDescendants;
		Vector3 unnormalizedAxisOfRotation = Vector3.Cross(worldSource - center, worldTarget - center);
		float unnormalizedAxisOfRotationLength = unnormalizedAxisOfRotation.Length();
		if (MathUtil.IsZero(unnormalizedAxisOfRotationLength)) {
			return new BonePartialSolution {
				angularVelocity = Vector3.Zero,
				time = float.PositiveInfinity
			};
		}
		Vector3 axisOfRotation = unnormalizedAxisOfRotation / unnormalizedAxisOfRotationLength;
		float momentOfInertia = massMoments[bone.Index].GetMomentOfInertia(axisOfRotation, center);

		var angularVelocity = torque / momentOfInertia;

		var twistAxis = bone.RotationOrder.TwistAxis;
		var existingRotation = bone.GetOrientedSpaceRotation(inputs).AsQuaternion(twistAxis);
		var relaxedRotation = bone.Constraint.Center.AsQuaternion(twistAxis);
		float relaxationBias = InverseKinematicsUtilities.CalculateRelaxationBias(relaxedRotation, existingRotation, angularVelocity);
		angularVelocity *= relaxationBias;

		var linearVelocity = Vector3.Cross(angularVelocity, boneSpaceSource);

		var rotation = QuaternionExtensions.RotateBetween(
			Vector3.Normalize(boneSpaceSource),
			Vector3.Normalize(boneSpaceTarget));
		var radius = boneSpaceSource.Length();
		var distance = rotation.AccurateAngle() * radius;

		float time = distance == 0 ? 0 : distance / linearVelocity.Length();

		DebugUtilities.AssertFinite(angularVelocity);

		return new BonePartialSolution {
			angularVelocity = angularVelocity,
			time = time
		};
	}

	private void ApplyPartialSolution(RigidBone bone, BonePartialSolution partialSolution, RigidTransform[] boneTransforms, Vector3 figureCenterOverride, RigidBoneSystemInputs inputs, float time) {
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
	
	private Vector3[] GetCentersOfMass(RigidTransform[] totalTransforms) {
		float[] descendantMasses = new float[boneSystem.Bones.Length];
		Vector3[] descendantMassPositions = new Vector3[boneSystem.Bones.Length];
		Vector3[] centersOfMass = new Vector3[boneSystem.Bones.Length];

		foreach (var bone in boneSystem.Bones.Reverse()) {
			float mass = boneAttributes[bone.Index].MassMoment.Mass;
			var position = totalTransforms[bone.Index].Transform(bone.CenterPoint + boneAttributes[bone.Index].MassMoment.GetCenterOfMass());
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

	private MassMoment[] GetMassMoments(RigidTransform[] totalTransforms, RigidBone[] boneChain) {
		MassMoment[] accumulators = new MassMoment[boneSystem.Bones.Length];

		bool[] areOnChain = new bool[boneSystem.Bones.Length];
		areOnChain[0] = true; //root bone is always on the chain
		foreach (var bone in boneChain) {
			areOnChain[bone.Index] = true;
		}

		foreach (var bone in boneSystem.Bones.Reverse()) {
			var unposedBoneCenteredMassMoment = boneAttributes[bone.Index].MassMoment;
			var unposedMassMoment = unposedBoneCenteredMassMoment.Translate(bone.CenterPoint);
			var totalTransform = totalTransforms[bone.Index];
			var massMoment = unposedMassMoment.Rotate(totalTransform.Rotation).Translate(totalTransform.Translation);
			var centerOfRation = totalTransform.Transform(bone.CenterPoint);

			accumulators[bone.Index].AddInplace(massMoment);
			
			var parent = bone.Parent;
			if (parent != null) {
				float counterRotationRatio;
				if (areOnChain[bone.Index]) {
					counterRotationRatio = 0;
				} else {
					counterRotationRatio = boneAttributes[bone.Index].MassMoment.Mass / boneAttributes[0].MassIncludingDescendants;
				}

				accumulators[parent.Index].AddFlexibleInPlace(accumulators[bone.Index], counterRotationRatio, centerOfRation);
			}
		}

		return accumulators;
	}

	private void CountertransformOffChainBones(RigidTransform[] preTotalTransforms, Vector3[] preCentersOfMass, RigidBoneSystemInputs inputs, RigidBone[] boneChain) {
		bool[] areOnChain = new bool[boneSystem.Bones.Length];
		areOnChain[0] = true; //root bone is always on the chain
		foreach (var bone in boneChain) {
			areOnChain[bone.Index] = true;
		}
		
		var rootTransform = RigidTransform.FromTranslation(inputs.RootTranslation);
		RigidTransform[] postTotalTransforms = new RigidTransform[preTotalTransforms.Length];

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
				float shrinkRatio = boneAttributes[bone.Index].MassMoment.Mass / boneAttributes[0].MassIncludingDescendants;
				var shrunkCounterRotation = Quaternion.Lerp(originalRotation, counterRotation, shrinkRatio);
				bone.SetRotation(inputs, shrunkCounterRotation, true);
			}

			var finalPostTotalTransform = bone.GetChainedTransform(inputs, parentPostTotalTransform);
			postTotalTransforms[bone.Index] = finalPostTotalTransform;
		}
	}

	private void ApplyOrientationGoal(InverseKinematicsGoal goal, RigidBoneSystemInputs inputs) {
		if (!areOrientable[goal.SourceBone.Index] || !goal.HasOrientation) {
			return;
		}

		var bone = goal.SourceBone;
		var parentBone = bone.Parent;
		var grandparentBone = parentBone.Parent;

		var grandparentTotalTransform = grandparentBone.GetChainedTransform(inputs);
		var parentTotalTransform = parentBone.GetChainedTransform(inputs, grandparentTotalTransform);

		var goalTotalRotation = Quaternion.Invert(goal.UnposedSourceOrientation).Chain(goal.TargetOrientation);
		
		var newLocalRotation = goalTotalRotation.Chain(Quaternion.Invert(parentTotalTransform.Rotation));
		bone.SetRotation(inputs, newLocalRotation, true);
		
		var clampedNewLocalRotation = bone.GetRotation(inputs);
		var residualGoalTotalRotation = Quaternion.Invert(clampedNewLocalRotation).Chain(goalTotalRotation);
		var parentNewLocalRotation = residualGoalTotalRotation.Chain(Quaternion.Invert(grandparentTotalTransform.Rotation));
		parentBone.SetTwistOnly(inputs, parentNewLocalRotation);
	}

	private void ApplyPositionGoal(InverseKinematicsGoal goal, RigidBoneSystemInputs inputs) {
		var boneTransforms = boneSystem.GetBoneTransforms(inputs);
		var centersOfMass = GetCentersOfMass(boneTransforms);
		
		var sourcePosition = boneTransforms[goal.SourceBone.Index].Transform(goal.UnposedSourcePosition);

		var bones = GetBoneChain(goal.SourceBone, goal.HasOrientation).ToArray();
		//var bones = new RigidBone[] { boneSystem.BonesByName["lForearmBend"], boneSystem.BonesByName["lShldrBend"] };
		
		var massMoments = GetMassMoments(boneTransforms, bones);
		var figureCenterOverride = massMoments[0].GetCenterOfMass();

		float totalRate = 0;

		var bonePartialSolutions = new BonePartialSolution[bones.Length];
		for (int i = 0; i < bones.Length; ++i) {
			var partialSolution = SolveSingleBone(bones[i], sourcePosition, goal.TargetPosition, massMoments, figureCenterOverride, inputs, boneTransforms);

			bonePartialSolutions[i] = partialSolution;
			totalRate += 1 / partialSolution.time;
		}

		var rootTranslationPartialSolution = SolveRootTranslation(sourcePosition, goal.TargetPosition);
		totalRate += 1 / rootTranslationPartialSolution.time;

		float time = 1 / totalRate;

		for (int i = 0; i < bones.Length; ++i) {
			ApplyPartialSolution(bones[i], bonePartialSolutions[i], boneTransforms, figureCenterOverride, inputs, time);
		}
		ApplyPartialSolution(rootTranslationPartialSolution, inputs, time);

		CountertransformOffChainBones(boneTransforms, centersOfMass, inputs, bones);
	}

	private void DoIteration(int iteration, InverseKinematicsGoal goal, RigidBoneSystemInputs inputs) {
		ApplyOrientationGoal(goal, inputs);
		ApplyPositionGoal(goal, inputs);
	}
	
	public void Solve(RigidBoneSystem boneSystem, List<InverseKinematicsGoal> goals, RigidBoneSystemInputs inputs) {
		for (int i = 0; i < Iterations; ++i) {
			foreach (var goal in goals) {
				DoIteration(i, goal, inputs);
			}
		}
	}
}