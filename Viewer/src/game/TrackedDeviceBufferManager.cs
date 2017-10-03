using SharpDX.Direct3D11;
using System;
using Valve.VR;

public class TrackedDeviceBufferManager : IDisposable {
	public TrackedDeviceBufferManager(Device device) {
	}

	public void Dispose() {
	}

	private TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

	public void Update(FrameUpdateParameters updateParameters) {
		poses = updateParameters.GamePoses;
	}

	public TrackedDevicePose_t GetPose(uint deviceIdx) {
		return poses[deviceIdx];
	}
}