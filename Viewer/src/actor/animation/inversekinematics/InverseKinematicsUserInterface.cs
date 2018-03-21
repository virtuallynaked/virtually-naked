using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Valve.VR;

public class InverseKinematicsUserInterface : IInverseKinematicsGoalProvider {
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly InverterParameters inverterParameters;
	private readonly DeviceTracker[] deviceTrackers;
		
	public InverseKinematicsUserInterface(ControllerManager controllerManager, ChannelSystem channelSystem, RigidBoneSystem boneSystem, InverterParameters inverterParameters) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;
		this.inverterParameters = inverterParameters;

		deviceTrackers = controllerManager.StateTrackers
			.Select(stateTracker => new DeviceTracker(this, stateTracker))
			.ToArray();
	}

	private class DeviceTracker {
		private readonly InverseKinematicsUserInterface parentInstance;
		private readonly ControllerStateTracker stateTracker;
		private bool tracking = false;
		private RigidBone sourceBone;
		private Vector3 boneRelativeSourcePosition;
		private Quaternion boneRelativeSourceOrientation;

		public DeviceTracker(InverseKinematicsUserInterface parentInstance, ControllerStateTracker stateTracker) {
			this.parentInstance = parentInstance;
			this.stateTracker = stateTracker;
		}

		private void MaybeStartTracking(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
			if (tracking == true) {
				//already tracking
				return;
			}

			if (!stateTracker.NonMenuActive) {
				return;
			}
				
			bool triggerPressed = stateTracker.IsPressed(EVRButtonId.k_EButton_SteamVR_Trigger);
			if (!triggerPressed) {
				return;
			}

			tracking = true;
				
			TrackedDevicePose_t gamePose = updateParameters.GamePoses[stateTracker.DeviceIdx];
			Matrix controllerTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
			DualQuaternion controllerTransformDq = DualQuaternion.FromMatrix(controllerTransform); 
			var worldSourcePosition = controllerTransformDq.Translation * 100;
			var worldSourceOrientation = controllerTransformDq.Rotation;

			sourceBone = parentInstance.MapPositionToBone(worldSourcePosition, previousFrameControlVertexInfos);
			var inverseSourceBoneTotalTransform = sourceBone.GetChainedTransform(inputs).Invert();
			boneRelativeSourcePosition = inverseSourceBoneTotalTransform.Transform(worldSourcePosition) - sourceBone.CenterPoint;
			boneRelativeSourceOrientation = worldSourceOrientation.Chain(inverseSourceBoneTotalTransform.Rotation);
		}

		private InverseKinematicsGoal MaybeContinueTracking(FrameUpdateParameters updateParameters) {
			if (!tracking) {
				return null;
			}

			if (!stateTracker.NonMenuActive) {
				tracking = false;
				return null;
			}

			bool triggerPressed = stateTracker.IsPressed(EVRButtonId.k_EButton_SteamVR_Trigger);
			if (!triggerPressed) {
				tracking = false;
				return null;
			}

			TrackedDevicePose_t gamePose = updateParameters.GamePoses[stateTracker.DeviceIdx];
			Matrix controllerTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
			var controllerTransformDq = DualQuaternion.FromMatrix(controllerTransform);
			var targetPosition = controllerTransformDq.Translation * 100;
			var targetOrientation = controllerTransformDq.Rotation;

			return new InverseKinematicsGoal(sourceBone,
				boneRelativeSourcePosition, boneRelativeSourceOrientation,
				targetPosition, targetOrientation);
		}

		public InverseKinematicsGoal GetGoal(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
			MaybeStartTracking(updateParameters, inputs, previousFrameControlVertexInfos);
			var goal =  MaybeContinueTracking(updateParameters);
			return goal;
		}
	}

	private RigidBone MapPositionToBone(Vector3 position, ControlVertexInfo[] previousFrameControlVertexInfos) {
		Vector3[] previousFrameControlVertexPositions = previousFrameControlVertexInfos.Select(vertexInfo => vertexInfo.position).ToArray();
		int faceIdx = ClosestPoint.FindClosestFaceOnMesh(inverterParameters.ControlFaces, previousFrameControlVertexPositions, position);
		int boneIdx = inverterParameters.ControlFaceToBoneMap[faceIdx];
		var bone = boneSystem.Bones[boneIdx];
		return bone;
	}
	
	public List<InverseKinematicsGoal> GetGoals(FrameUpdateParameters updateParameters, RigidBoneSystemInputs inputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		List<InverseKinematicsGoal> goals = new List<InverseKinematicsGoal>();
		foreach (var deviceTracker in deviceTrackers) {
			var goal = deviceTracker.GetGoal(updateParameters, inputs, previousFrameControlVertexInfos);
			if (goal != null) {
				goals.Add(goal);
			}
		}
		return goals;
	}
}
