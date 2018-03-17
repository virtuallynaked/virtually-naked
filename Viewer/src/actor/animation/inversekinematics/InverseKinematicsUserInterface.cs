using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Valve.VR;

public class InverseKinematicsUserInterface : IInverseKinematicsGoalProvider {
	private static Dictionary<string, string> FaceGroupGrabMap = new Dictionary<string, string> {
		["lThumb3"] = "lHand",
		["lIndex3"] = "lHand",
		["lMid3"] = "lHand",
		["lRing3"] = "lHand",
		["lPinky3"] = "lHand",
		["rThumb3"] = "rHand",
		["rIndex3"] = "rHand",
		["rMid3"] = "rHand",
		["rRing3"] = "rHand",
		["rPinky3"] = "rHand",
		["lPectoral"] = "Chest",
		["rPectoral"] = "Chest",
		["lEye"] = "Head",
		["rEye"] = "Head",
		["LowerJaw"] = "Head",
		["UpperJaw"] = "Head",
		["Tongue"] = "Head",
		["Neck"] = "ChestUpper",
		["lThumb1"] = "lHand",
		["lIndex1"] = "lHand",
		["lIndex2"] = "lHand",
		["lMid2"] = "lHand",
		["lMid1"] = "lHand",
		["lPinky2"] = "lHand",
		["lPinky1"] = "lHand",
		["lThumb2"] = "lHand",
		["lRing2"] = "lHand",
		["lRing1"] = "lHand",
		["rThumb1"] = "rHand",
		["rIndex1"] = "rHand",
		["rIndex2"] = "rHand",
		["rMid2"] = "rHand",
		["rMid1"] = "rHand",
		["rPinky2"] = "rHand",
		["rPinky1"] = "rHand",
		["rThumb2"] = "rHand",
		["rRing2"] = "rHand",
		["rRing1"] = "rHand",
		["lToe"] = "lFoot",
		["rToe"] = "rFoot",
	};

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
			boneRelativeSourcePosition = inverseSourceBoneTotalTransform.Transform(worldSourcePosition);
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
		int faceGroupIdx = inverterParameters.FaceGroupMap[faceIdx];
		string faceGroupName = inverterParameters.FaceGroupNames[faceGroupIdx];
		
		if (!FaceGroupGrabMap.TryGetValue(faceGroupName, out string grabFaceGroupName)) {
			grabFaceGroupName = faceGroupName;
		}

		string boneName = inverterParameters.FaceGroupToNodeMap[grabFaceGroupName];
		return boneSystem.BonesByName[boneName];
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
