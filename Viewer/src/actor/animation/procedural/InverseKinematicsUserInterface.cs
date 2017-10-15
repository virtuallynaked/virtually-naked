using SharpDX;
using System.Collections.Generic;
using System.Linq;
using Valve.VR;

public class InverseKinematicsUserInterface {
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
		["rRing1"] = "rHand"
	};

	private readonly ControllerManager controllerManager;
	private readonly ChannelSystem channelSystem;
	private readonly RigidBoneSystem boneSystem;
	private readonly InverterParameters inverterParameters;

	private bool tracking = false;
	private uint trackedDeviceIdx;
	private RigidBone sourceBone;
	private Vector3 boneRelativeSourcePosition;

	public InverseKinematicsUserInterface(ControllerManager controllerManager, ChannelSystem channelSystem, RigidBoneSystem boneSystem, InverterParameters inverterParameters) {
		this.controllerManager = controllerManager;
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;
		this.inverterParameters = inverterParameters;
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

	public InverseKinematicsProblem GetProblem(FrameUpdateParameters updateParameters, ChannelOutputs outputs, ControlVertexInfo[] previousFrameControlVertexInfos) {
		if (!tracking) {
			for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
				ControllerStateTracker stateTracker = controllerManager.StateTrackers[deviceIdx];
				if (!stateTracker.NonMenuActive) {
					continue;
				}
				
				bool triggerPressed = stateTracker.IsPressed(EVRButtonId.k_EButton_SteamVR_Trigger);
				if (!triggerPressed) {
					continue;
				}

				tracking = true;
				trackedDeviceIdx = deviceIdx;
				
				TrackedDevicePose_t gamePose = updateParameters.GamePoses[deviceIdx];
				Matrix controllerTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
				var worldSourcePosition = controllerTransform.TranslationVector * 100;

				sourceBone = MapPositionToBone(worldSourcePosition, previousFrameControlVertexInfos);
				boneRelativeSourcePosition = sourceBone.GetChainedTransform(outputs).InverseTransform(worldSourcePosition);
			}
		}

		if (tracking) {
			ControllerStateTracker stateTracker = controllerManager.StateTrackers[trackedDeviceIdx];

			if (!stateTracker.NonMenuActive) {
				tracking = false;
				return null;
			}

			bool triggerPressed = stateTracker.IsPressed(EVRButtonId.k_EButton_SteamVR_Trigger);
			if (!triggerPressed) {
				tracking = false;
				return null;
			}

			TrackedDevicePose_t gamePose = updateParameters.GamePoses[trackedDeviceIdx];
			Matrix controllerTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
			var targetPosition = controllerTransform.TranslationVector * 100;

			return new InverseKinematicsProblem(sourceBone, boneRelativeSourcePosition, targetPosition);
		} else {
			return null;
		}
	}
}
