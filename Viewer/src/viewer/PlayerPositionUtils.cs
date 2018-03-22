using SharpDX;
using Valve.VR;

public static class PlayerPositionUtils {
	public static Vector3 GetHeadPosition(TrackedDevicePose_t hmdPose) {
		Matrix hmdToWorldTransform = hmdPose.mDeviceToAbsoluteTracking.Convert();

		//The HMD position is midway between the lenses, so I need to go back and down a bit to the to the center of the player's face
		Vector3 headPositionInHmdSpace = new Vector3(0, -0.01f, +0.04f);

		Vector3 headPositionInWorldSpace = Vector3.TransformCoordinate(headPositionInHmdSpace, hmdToWorldTransform);
		return headPositionInWorldSpace;
	}

	public static Vector3 GetHeadPosition(TrackedDevicePose_t[] poses) {
		return GetHeadPosition(poses[OpenVR.k_unTrackedDeviceIndex_Hmd]);
	}
}
