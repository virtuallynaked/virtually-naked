using System;
using SharpDX;
using Valve.VR;
using System.Linq;

public class MenuController {
	private readonly MenuModel model;
	private readonly ControllerManager controllerManager;
	
	private readonly Vector2[] velocities;

	public MenuController(MenuModel model, ControllerManager controllerManager) {
		this.model = model;
		this.controllerManager = controllerManager;
		velocities = new Vector2[OpenVR.k_unMaxTrackedDeviceCount];
	}
			
	private void ApplyDelta(Vector2 delta, uint deviceIdx, uint axisIdx) {
		int pulseDuration = model.Move(delta);
		if (pulseDuration > 0) {
			OpenVR.System.TriggerHapticPulse(deviceIdx, axisIdx, (ushort) pulseDuration);
		}
	}

	private static float PowerCurve(float x, float exponent) {
		return Math.Sign(x) * (float) Math.Pow(Math.Abs(x), exponent);
	}

	private static Vector2 PowerCurve(Vector2 v, float exponent) {
		return new Vector2(
			PowerCurve(v.X, exponent),
			PowerCurve(v.Y, exponent));
	}

	public void Update() {
		for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
			ControllerStateTracker controllerStateTracker = controllerManager.StateTrackers[deviceIdx];
			
			if (!controllerStateTracker.MenuActive) {
				continue;
			}

			uint primaryAxisIdx = 0;
			EVRButtonId primaryAxisButtonId = EVRButtonId.k_EButton_Axis0 + (int) primaryAxisIdx;

			if (controllerStateTracker.WasClicked(primaryAxisButtonId) ||
				controllerStateTracker.WasClicked(EVRButtonId.k_EButton_A)) { //k_EButton_A is the A/X button on touch
				model.Press();
			}
			
			if (controllerStateTracker.WasClicked(EVRButtonId.k_EButton_Grip)) {
				model.PressSecondary();
			}
			
			EVRControllerAxisType axisType = (EVRControllerAxisType) OpenVR.System.GetInt32TrackedDeviceProperty(deviceIdx,
				ETrackedDeviceProperty.Prop_Axis0Type_Int32 + (int) primaryAxisIdx);

			if (axisType == EVRControllerAxisType.k_eControllerAxis_TrackPad) {
				//use interial scrolling
				if (controllerStateTracker.HasTouchDelta(primaryAxisIdx)) {
					Vector2 delta = controllerStateTracker.GetTouchDelta(primaryAxisIdx);
					velocities[deviceIdx] = delta / controllerStateTracker.GetUpdateRate();
					ApplyDelta(delta, deviceIdx, primaryAxisIdx);
				} else if (!controllerStateTracker.IsTouched(primaryAxisButtonId)) {
					Vector2 velocity = velocities[deviceIdx];
					if (velocity != Vector2.Zero) {
						if (velocity.LengthSquared() < 1e-5f) {
							velocity = Vector2.Zero;
							model.DoneMove();
						} else {
							velocity *= 0.99f;
							ApplyDelta(velocity, deviceIdx, primaryAxisIdx);
						}
						velocities[deviceIdx] = velocity;
					}
				}
			} else {
				//use velocity scrolling
				if (controllerStateTracker.IsTouched(primaryAxisButtonId)) {
					Vector2 position = controllerStateTracker.GetAxisPosition(primaryAxisIdx);
					Vector2 velocity = PowerCurve(position, 2) * 0.2f;
					Vector2 delta = velocity;
					ApplyDelta(delta, deviceIdx, primaryAxisIdx);
				} else if (controllerStateTracker.BecameUntouched(primaryAxisButtonId)) {
					model.DoneMove();
				}
			}
		}
	}
}
