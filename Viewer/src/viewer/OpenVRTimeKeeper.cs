using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

public class OpenVRTimeKeeper {
	private readonly float secondsFromVSyncToPhotons;
	private readonly float secondsPerFrame;

	private float timeDelta;
	private float time;
	
	public OpenVRTimeKeeper() {
		this.secondsFromVSyncToPhotons = OpenVR.System.GetFloatTrackedDeviceProperty(
			OpenVR.k_unTrackedDeviceIndex_Hmd,
			ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float);
		float framesPerSecond = OpenVR.System.GetFloatTrackedDeviceProperty(
			OpenVR.k_unTrackedDeviceIndex_Hmd,
			ETrackedDeviceProperty.Prop_DisplayFrequency_Float);
		this.secondsPerFrame = 1 / framesPerSecond;
	}

	public void AdvanceFrame() {
		// Handle asynchronous reprojection: https://steamcommunity.com/app/358720/discussions/0/385429254937377076/
		var timing = OpenVR.Compositor.GetFrameTiming(0);
		timeDelta = timing.m_nNumFramePresents * secondsPerFrame;

		time += timeDelta;
	}

	public float GetNextFrameTime(int framesAhead) {
		return time + framesAhead * timeDelta + secondsFromVSyncToPhotons;
	}
}
