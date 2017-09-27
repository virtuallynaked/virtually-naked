using System;
using Valve.VR;

class OpenVRKeyboardHelper {
	private static Action<string> pendingCallback = null;

	public static void PromptForString(EGamepadTextInputMode eInputMode, EGamepadTextInputLineMode eLineInputMode, string pchDescription, uint unCharMax, string pchExistingText, Action<string> callback) {
		pendingCallback = callback;
		OpenVR.Overlay.ShowKeyboard((int) eInputMode, (int) eLineInputMode, pchDescription, unCharMax, pchExistingText, false, 0L);
	}

	public static void ProcessEvent(VREvent_t vrEvent) {
		EVREventType eventType = (EVREventType) vrEvent.eventType;
		if (eventType == EVREventType.VREvent_KeyboardDone) {
			string text = OpenVR.Overlay.GetKeyboardText();
			if (pendingCallback != null) {
				var callback = pendingCallback;
				pendingCallback = null;
				callback.Invoke(text);
			}
		}
	}
}