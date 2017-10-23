using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.IO;
using Valve.VR;

public class FramePreparerDemo : IDemoApp {
	private void Run(FramePreparer framePreparer) {
		float time = 0;
		float deltaTime = 1 / 90f;
		TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		OpenVR.Compositor.GetLastPoses(gamePoses, gamePoses);

		Vector3 headPosition = new Vector3(0, 1.5f, 1f);

		var stopwatch = Stopwatch.StartNew();
		int frameCount = 0;

		while (true) {
			time += deltaTime;
			var updateParameters = new FrameUpdateParameters(time, deltaTime, gamePoses, headPosition);
			var preparedFrame = framePreparer.PrepareFrame(updateParameters);
			preparedFrame.Dispose();

			frameCount += 1;
			if (frameCount == 100) {
				Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds / frameCount);

				frameCount = 0;
				stopwatch.Restart();
			}
		}

	}

	public void Run() {
		var dataDir = UnpackedArchiveDirectory.Make(new DirectoryInfo("work"));
		var device = new Device(DriverType.Hardware, DeviceCreationFlags.None, FeatureLevel.Level_11_1);
		var shaderCache = new ShaderCache(device);
		var standardSamplers = new StandardSamplers(device);
		var targetSize = new Size2(1024, 1024);

		using (var framePreparer = new FramePreparer(dataDir, device, shaderCache, standardSamplers, targetSize)) {
			OpenVRExtensions.Init(EVRApplicationType.VRApplication_Other);
			Run(framePreparer);
			OpenVR.Shutdown();
		}
	}
}