using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

static class OpenVRExtensions {
	public static string GetStringTrackedDeviceProperty(this CVRSystem system, uint unDeviceIndex, ETrackedDeviceProperty prop) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;

		uint length = system.GetStringTrackedDeviceProperty(unDeviceIndex, prop, null, 0, ref error);

		StringBuilder builder = new StringBuilder((int) length);

		system.GetStringTrackedDeviceProperty(unDeviceIndex, prop, builder, length, ref error);
		if (error != ETrackedPropertyError.TrackedProp_Success) {
			throw OpenVRException.Make(error);
		}

		return builder.ToString();
	}

	public static float GetFloatTrackedDeviceProperty(this CVRSystem system, uint unDeviceIndex, ETrackedDeviceProperty prop) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		float value = system.GetFloatTrackedDeviceProperty(unDeviceIndex, prop, ref error);
		if (error != ETrackedPropertyError.TrackedProp_Success) {
			throw OpenVRException.Make(error);
		}
		return value;
	}

	public static int GetInt32TrackedDeviceProperty(this CVRSystem system, uint unDeviceIndex, ETrackedDeviceProperty prop) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		int value = system.GetInt32TrackedDeviceProperty(unDeviceIndex, prop, ref error);
		if (error != ETrackedPropertyError.TrackedProp_Success) {
			throw OpenVRException.Make(error);
		}
		return value;
	}

	public static string GetComponentName(this CVRRenderModels renderModels, string pchRenderModelName, uint unComponentIndex) {
		uint length = renderModels.GetComponentName(pchRenderModelName, unComponentIndex, null, 0);
		if (length == 0) {
			return String.Empty;
		}
		StringBuilder builder = new StringBuilder((int) length);
		renderModels.GetComponentName(pchRenderModelName, unComponentIndex, builder, length);
		return builder.ToString();
	}

	public static string GetComponentRenderModelName(this CVRRenderModels renderModels, string pchRenderModelName, string pchComponentName) {
		uint length = renderModels.GetComponentRenderModelName(pchRenderModelName, pchComponentName, null, 0);
		if (length == 0) {
			return null;
		}
		StringBuilder builder = new StringBuilder((int) length);
		renderModels.GetComponentRenderModelName(pchRenderModelName, pchComponentName, builder, length);
		return builder.ToString();
	}

	private static readonly uint CONTROLLER_STATE_SIZE = (uint) Marshal.SizeOf<VRControllerState_t>();

	public static bool GetControllerState(this CVRSystem system, uint unControllerDeviceIndex, out VRControllerState_t pControllerState) {
		pControllerState = default(VRControllerState_t);
		return system.GetControllerState(unControllerDeviceIndex, ref pControllerState, CONTROLLER_STATE_SIZE);
	}

	public static bool GetControllerStateWithPose(this CVRSystem system, ETrackingUniverseOrigin eOrigin, uint unControllerDeviceIndex, out VRControllerState_t pControllerState, out TrackedDevicePose_t pTrackedDevicePose) {
		pControllerState = default(VRControllerState_t);
		pTrackedDevicePose = default(TrackedDevicePose_t);
		return system.GetControllerStateWithPose(eOrigin, unControllerDeviceIndex, ref pControllerState, CONTROLLER_STATE_SIZE, ref pTrackedDevicePose);
	}

	public static Matrix Convert(this HmdMatrix34_t mat) {
		return new Matrix(
			mat.m0, mat.m4, mat.m8, 0.0f, 
			mat.m1, mat.m5, mat.m9, 0.0f,
			mat.m2, mat.m6, mat.m10, 0.0f,
			mat.m3, mat.m7, mat.m11, 1.0f);
	}
	
	public static Matrix Convert(this HmdMatrix44_t mat) {
		return new Matrix(
			mat.m0, mat.m4, mat.m8, mat.m12, 
			mat.m1, mat.m5, mat.m9, mat.m13,
			mat.m2, mat.m6, mat.m10, mat.m14,
			mat.m3, mat.m7, mat.m11, mat.m15);
	}

	public static Vector3 Convert(this HmdVector3_t vec) {
		return new Vector3(vec.v0, vec.v1, vec.v2);
	}

	private static readonly uint EVENT_SIZE = (uint) Marshal.SizeOf<VREvent_t>();

	public static bool PollNextEvent(this CVRSystem system, ref VREvent_t pEvent) {
		return system.PollNextEvent(ref pEvent, EVENT_SIZE);
	}

	public static bool IsPressed(this VRControllerState_t controllerState, EVRButtonId buttonId) {
		return (controllerState.ulButtonPressed & (1ul << (int) buttonId)) != 0;
	}

	public static bool IsTouched(this VRControllerState_t controllerState, EVRButtonId buttonId) {
		return (controllerState.ulButtonTouched & (1ul << (int) buttonId)) != 0;
	}

	public static VRControllerAxis_t GetAxis(this VRControllerState_t controllerState, uint axisIdx) {
		switch (axisIdx) {
			case 0:
				return controllerState.rAxis0;
			case 1:
				return controllerState.rAxis1;
			case 2:
				return controllerState.rAxis2;
			case 3:
				return controllerState.rAxis3;
			case 4:
				return controllerState.rAxis4;
			default:
				throw new IndexOutOfRangeException();
		}
	}

	public static Vector2 AsVector(this VRControllerAxis_t axis) {
		return new Vector2(axis.x, axis.y);
	}

	public static Size2 GetRecommendedRenderTargetSize(this CVRSystem system) {
		uint pnWidth = 0, pnHeight = 0;
		system.GetRecommendedRenderTargetSize(ref pnWidth, ref pnHeight);
		return new Size2((int) pnWidth, (int) pnHeight);
	}

	private static readonly uint FRAMETIMING_SIZE = (uint) Marshal.SizeOf<Compositor_FrameTiming>();

	public static Compositor_FrameTiming GetFrameTiming(this CVRCompositor compositor, uint unFramesAgo) {
		Compositor_FrameTiming pTiming = default(Compositor_FrameTiming);
		pTiming.m_nSize = FRAMETIMING_SIZE;
		OpenVR.Compositor.GetFrameTiming(ref pTiming, unFramesAgo);
		return pTiming;
	}

	public static void Init(EVRApplicationType eApplicationType = EVRApplicationType.VRApplication_Scene) {
		EVRInitError peError = EVRInitError.None;
		OpenVR.Init(ref peError, eApplicationType);
		if (peError != EVRInitError.None) {
			throw OpenVRException.Make(peError);
		}
	}

	public static string GetKeyboardText(this CVROverlay overlay) {
		uint length = overlay.GetKeyboardText(null, 0) + 1;
		StringBuilder builder = new StringBuilder((int) length);
		overlay.GetKeyboardText(builder, length);
		return builder.ToString();
	}
}
