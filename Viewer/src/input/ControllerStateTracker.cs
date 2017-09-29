using SharpDX;
using System;
using Valve.VR;

public class ControllerStateTracker {
	private readonly uint deviceIdx;

	private bool active;
	private bool menuOpen;
	private VRControllerState_t secondPreviousState;
	private VRControllerState_t previousState;
	private VRControllerState_t currentState;
	private int staleness;
	private int previousStaleness;

	public ControllerStateTracker(uint deviceIdx) {
		this.deviceIdx = deviceIdx;
	}

	public void Update() {
		if (OpenVR.System.GetTrackedDeviceClass(deviceIdx) != ETrackedDeviceClass.Controller) {
			active = false;
			return;
		}

		if (!OpenVR.System.GetControllerState(deviceIdx, out var controllerState)) {
			active = false;
			return;
		}

		if (active) {
			staleness += 1;
			if (controllerState.unPacketNum == currentState.unPacketNum) {
				return;
			}
		}

		if (!active) {
			//activate
			Console.WriteLine("activate");
			active = true;
			secondPreviousState = default(VRControllerState_t);
			previousState = default(VRControllerState_t);
		} else {
			secondPreviousState = previousState;
			previousState = currentState;
			previousStaleness = staleness;
		}

		currentState = controllerState;
		staleness = 0;

		if (WasClicked(EVRButtonId.k_EButton_ApplicationMenu)) {
			menuOpen = !menuOpen;
		}
	}

	public bool Active => active;
	public bool IsFresh => staleness == 0;
	public bool MenuActive => active && menuOpen;
	public bool NonMenuActive => active && !menuOpen;

	public bool WasClicked(EVRButtonId buttonId) {
		return staleness == 0 && previousState.IsPressed(buttonId) && !currentState.IsPressed(buttonId);
	}

	public bool IsTouched(EVRButtonId buttonId) {
		return currentState.IsTouched(buttonId);
	}

	public bool IsPressed(EVRButtonId buttonId) {
		return currentState.IsPressed(buttonId);
	}

	public bool BecameUntouched(EVRButtonId buttonId) {
		return staleness == 0 && previousState.IsTouched(buttonId) && !currentState.IsTouched(buttonId);
	}

	public bool HasTouchDelta(uint axisIdx) {
		EVRButtonId buttonId = EVRButtonId.k_EButton_Axis0 + (int) axisIdx;

		if (staleness > 0) {
			return false;
		}

		if (!currentState.IsTouched(buttonId) || !previousState.IsTouched(buttonId)) {
			return false;
		}

		if (!secondPreviousState.IsTouched(buttonId)) {
			//workaround for SteamVR bug: http://steamcommunity.com/app/250820/discussions/3/2132869574256358055/
			return false;
		}

		return true;
	}

	public Vector2 GetTouchDelta(uint axisIdx) {
		Vector2 previousPosition = previousState.GetAxis(axisIdx).AsVector();
		Vector2 currentPosition = currentState.GetAxis(axisIdx).AsVector();
		return currentPosition - previousPosition;
	}
	
	public Vector2 GetAxisPosition(uint axisIdx) {
		return currentState.GetAxis(axisIdx).AsVector();
	}

	public int GetUpdateRate() {
		return previousStaleness;
	}
}
