using System.Linq;
using Valve.VR;

public class ControllerManager {
	private readonly ControllerStateTracker[] controllerStateTrackers;

	public ControllerManager() {
		controllerStateTrackers = Enumerable.Range(0, (int) OpenVR.k_unMaxTrackedDeviceCount)
			.Select(deviceIdx => new ControllerStateTracker((uint) deviceIdx))
			.ToArray();
	}

	public ControllerStateTracker[] StateTrackers => controllerStateTrackers;

	public bool AnyMenuActive {
		get {
			bool anyActive = false;
			for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
				anyActive |= controllerStateTrackers[deviceIdx].MenuActive;
			}
			return anyActive;
		}
	}

	public void Update() {
		for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
			ControllerStateTracker controllerStateTracker = controllerStateTrackers[deviceIdx];
			controllerStateTracker.Update();
		}
	}
}
