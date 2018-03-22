using SharpDX;
using SharpDX.Direct3D11;
using System;
using Valve.VR;
using Buffer = SharpDX.Direct3D11.Buffer;

public class TrackedDeviceBufferManager : IDisposable {
	public const uint MaxDeviceCount = OpenVR.k_unMaxTrackedDeviceCount;

	private readonly CoordinateNormalMatrixPairConstantBufferManager[] objectSpaceToWorldTransformBufferManagers;

	public TrackedDeviceBufferManager(Device device) {
		objectSpaceToWorldTransformBufferManagers = new CoordinateNormalMatrixPairConstantBufferManager[MaxDeviceCount];
		for (int i = 0; i < MaxDeviceCount; ++i) {
			objectSpaceToWorldTransformBufferManagers[i] = new CoordinateNormalMatrixPairConstantBufferManager(device);
		}
	}

	public void Dispose() {
		foreach (var manager in objectSpaceToWorldTransformBufferManagers) {
			manager.Dispose();
		}
	}
	
	public void DoPrework(DeviceContext context, TrackedDevicePose_t[] poses) {
		for (int i = 0; i < MaxDeviceCount; ++i) {
			TrackedDevicePose_t pose = poses[i];
			Matrix objectToWorldSpaceTransform = pose.bPoseIsValid ? pose.mDeviceToAbsoluteTracking.Convert() : Matrix.Zero;
			objectSpaceToWorldTransformBufferManagers[i].Update(context, objectToWorldSpaceTransform);
		}
	}
	
	public Buffer GetObjectToWorldSpaceTransformBuffer(uint deviceIdx) {
		return objectSpaceToWorldTransformBufferManagers[deviceIdx].Buffer;
	}
}
