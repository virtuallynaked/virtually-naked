using SharpDX;
using Valve.VR;

class DragHandle {
	private const uint UnattachedSentinel = OpenVR.k_unMaxTrackedDeviceCount;

	private readonly ControllerManager controllerManager;

	private uint trackedDeviceIdx = UnattachedSentinel;
	private Matrix objectToControllerTransform;
	private Matrix objectToWorldTransform;

	public Matrix Transform {
		get {
			return objectToWorldTransform;
		}
		set {
			objectToWorldTransform = value;
		}
	}
	
	public DragHandle(ControllerManager controllerManager, Matrix initialTransform) {
		this.controllerManager = controllerManager;
		objectToWorldTransform = initialTransform;
	}

	public DragHandle(ControllerManager controllerManager) : this(controllerManager, Matrix.Identity) {
	}

	public void Update(FrameUpdateParameters updateParameters) {
		if (trackedDeviceIdx == UnattachedSentinel) {
			for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
				ControllerStateTracker stateTracker = controllerManager.StateTrackers[deviceIdx];
				if (!stateTracker.NonMenuActive) {
					continue;
				}
			
				if (!stateTracker.IsPressed(EVRButtonId.k_EButton_Grip)) {
					continue;
				}
			
				trackedDeviceIdx = deviceIdx;

				TrackedDevicePose_t gamePose = updateParameters.GamePoses[deviceIdx];
				Matrix controllerToWorldTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
			
				Matrix worldToControllerTransform = Matrix.Invert(controllerToWorldTransform);
			
				objectToControllerTransform = objectToWorldTransform * worldToControllerTransform;
			}
		}

		if (trackedDeviceIdx != UnattachedSentinel) {
			ControllerStateTracker stateTracker = controllerManager.StateTrackers[trackedDeviceIdx];

			if (!stateTracker.NonMenuActive || !stateTracker.IsPressed(EVRButtonId.k_EButton_Grip)) {
				trackedDeviceIdx = UnattachedSentinel;
				return;
			}

			TrackedDevicePose_t gamePose = updateParameters.GamePoses[trackedDeviceIdx];
			Matrix controllerToWorldTransform = gamePose.mDeviceToAbsoluteTracking.Convert();

			objectToWorldTransform = objectToControllerTransform * controllerToWorldTransform;
		}
	}
}
