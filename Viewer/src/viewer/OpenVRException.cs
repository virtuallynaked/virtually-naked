using System;
using Valve.VR;

public class OpenVRException : Exception {
	public static RenderModelException Make(EVRRenderModelError errorCode) {
		string message = OpenVR.RenderModels.GetRenderModelErrorNameFromEnum(errorCode);
		return new RenderModelException(message, errorCode);
	}

	public static TrackedPropertyException Make(ETrackedPropertyError errorCode) {
		string message = OpenVR.System.GetPropErrorNameFromEnum(errorCode);
		return new TrackedPropertyException(message, errorCode);
	}

	public static VRInitException Make(EVRInitError errorCode) {
		string message = OpenVR.GetStringForHmdError(errorCode);
		return new VRInitException(message, errorCode);
	}

	public OpenVRException(string message) : base(message) {
	}
}

public class RenderModelException : OpenVRException {
	private readonly EVRRenderModelError errorCode;

	public RenderModelException(string message, EVRRenderModelError errorCode) : base(message) {
		this.errorCode = errorCode;
	}
}

public class TrackedPropertyException : OpenVRException {
	private readonly ETrackedPropertyError errorCode;

	public TrackedPropertyException(string message, ETrackedPropertyError errorCode) : base(message) {
		this.errorCode = errorCode;
	}
}

public class VRInitException : OpenVRException {
	private readonly EVRInitError errorCode;

	public VRInitException(string message, EVRInitError errorCode) : base(message) {
		this.errorCode = errorCode;
	}
}