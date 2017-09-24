using SharpDX;
using Valve.VR;

public static class PlayerPositionUtils {
	public static Vector3 GetHeadPosition(TrackedDevicePose_t hmdPose) {
		Matrix hmdToWorldTransform = hmdPose.mDeviceToAbsoluteTracking.Convert();
		Vector3 headPositionInHmdSpace = new Vector3(0, -0.01f, +0.05f);
		Vector3 headPositionInWorldSpace = Vector3.TransformCoordinate(headPositionInHmdSpace, hmdToWorldTransform);
		return headPositionInWorldSpace;
	}

	public static Vector3 GetHeadPosition(TrackedDevicePose_t[] poses) {
		return GetHeadPosition(poses[OpenVR.k_unTrackedDeviceIndex_Hmd]);
	}
}
