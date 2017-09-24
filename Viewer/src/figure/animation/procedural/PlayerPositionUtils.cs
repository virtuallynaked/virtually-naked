using SharpDX;
using Valve.VR;

public static class PlayerPositionUtils {
	public static Vector3 GetHeadGamePosition() {
		TrackedDevicePose_t pose = default(TrackedDevicePose_t);
		TrackedDevicePose_t gamePose = default(TrackedDevicePose_t);
		OpenVR.Compositor.GetLastPoseForTrackedDeviceIndex(OpenVR.k_unTrackedDeviceIndex_Hmd, ref pose, ref gamePose);
		Matrix hmdToWorldTransform = gamePose.mDeviceToAbsoluteTracking.Convert();

		Vector3 headPositionInHmdSpace = new Vector3(0, -0.01f, +0.05f);
		Vector3 headPositionInWorldSpace = Vector3.TransformCoordinate(headPositionInHmdSpace, hmdToWorldTransform);
		return headPositionInWorldSpace;
	}
}
