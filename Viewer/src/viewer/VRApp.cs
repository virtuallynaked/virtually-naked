using System;
using SharpDX.Windows;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX;
using Valve.VR;
using SharpDX.Diagnostics;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

public class VRApp : IDisposable {
	private const bool debugDevice = false;

	private static void ReportUnhandledException(object sender, UnhandledExceptionEventArgs e) {
		using (var dialog = new ThreadExceptionDialog(e.ExceptionObject as Exception)) {
			dialog.ShowDialog();
		}
	}

	[STAThread]
	public static void Main(string[] args) {
		if (!Debugger.IsAttached) {
			AppDomain.CurrentDomain.UnhandledException += ReportUnhandledException;
		}

		var commandLineParser = new CommandLineApplication(false);
		var archiveOption = commandLineParser.Option("--data", "path to archive file or directory", CommandOptionType.SingleValue);
		commandLineParser.Execute(args);

		string archivePath;
		if (archiveOption.HasValue()) {
			archivePath = archiveOption.Value();
		} else {
			archivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.archive");
		}

		IArchiveDirectory dataDir;

		var archiveAsDirectory = new DirectoryInfo(archivePath);
		if (archiveAsDirectory.Exists) {
			dataDir = UnpackedArchiveDirectory.Make(archiveAsDirectory);
		} else {
			var archive = new PackedArchive(new FileInfo(archivePath));
			dataDir = archive.Root;
		}
		
		LeakTracking.Setup();
		
		string title = Application.ProductName + " " + Application.ProductVersion;
		
		try {
			using (VRApp app = new VRApp(dataDir, title)) {
				app.Run();
			}
		} catch (VRInitException e) {
			string text =String.Join("\n\n",
				String.Format("OpenVR initialization failed: {0}", e.Message),
				"Please make sure SteamVR is installed and running, and VR headset is connected.");
			string caption = "OpenVR Initialization Error";
			
			MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		LeakTracking.Finish();
	}
		
	private readonly CompanionWindow companionWindow;

	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly DeviceContext immediateContext;

	private OpenVRTimeKeeper timeKeeper;
	private TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	private TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

	private readonly HiddenAreaMeshes hiddenAreaMeshes;

	private readonly StandardSamplers standardSamplers;
	private readonly FramePreparer framePreparer;
	private readonly AsyncFramePreparer asyncFramePreparer;

	private IPreparedFrame preparedFrame;

	private static Device CreateDevice() {
		SharpDX.DXGI.Adapter chosenAdapter = null;

		using (var dxgiFactory = new SharpDX.DXGI.Factory1()) {
			var adapters = dxgiFactory.Adapters;
			try {
				ulong adapterLuid = OpenVR.System.GetOutputDevice(ETextureType.DirectX, IntPtr.Zero);
				if (adapterLuid != 0) {
					foreach (var adapter in adapters) {
						if ((ulong) adapter.Description.Luid == adapterLuid) {
							chosenAdapter = adapter;
						}
					}
				}

				if (chosenAdapter == null) {
					//fallback to the default adapter
					chosenAdapter = adapters[0];
				}

				var device = new Device(chosenAdapter, debugDevice ? DeviceCreationFlags.Debug : DeviceCreationFlags.None);
				return device;
			}
			finally {
				foreach (var adapter in adapters) {
					adapter.Dispose();
				}
			}
		}
	}

	public VRApp(IArchiveDirectory dataDir, string title) {
		OpenVRExtensions.Init();

		device = CreateDevice();
		shaderCache = new ShaderCache(device);
		standardSamplers = new StandardSamplers(device);
		
		companionWindow = new CompanionWindow(device, shaderCache, standardSamplers, title, dataDir);
				
		immediateContext = device.ImmediateContext;
				
		timeKeeper = new OpenVRTimeKeeper();
				
		hiddenAreaMeshes = new HiddenAreaMeshes(device);

		Size2 targetSize = OpenVR.System.GetRecommendedRenderTargetSize();
		framePreparer = new FramePreparer(dataDir, device, shaderCache, standardSamplers, targetSize);
		asyncFramePreparer = new AsyncFramePreparer(framePreparer);
	}
	
	public void Dispose() {
		preparedFrame?.Dispose();

		framePreparer.Dispose();

		standardSamplers.Dispose();
		hiddenAreaMeshes.Dispose();

		OpenVR.Shutdown();
		
		immediateContext.Dispose();
		
		companionWindow.Dispose();
		
		shaderCache.Dispose();
		device.Dispose();
	}
	
	private void Run() {
		//setup initial frame
		timeKeeper.Start();
		OpenVR.Compositor.GetLastPoses(poses, gamePoses);
		KickoffFramePreparation();
		preparedFrame = asyncFramePreparer.FinishPreparingFrame();

		RenderLoop.Run(companionWindow.Form, DoFrame);
	}
	
	private Texture2D mostRecentRenderTexture;
	private Matrix mostRecentProjectionTransform;

	private Texture2D RenderView(IPreparedFrame preparedFrame, HiddenAreaMesh hiddenAreaMesh, Matrix viewTransform, Matrix projectionTransform) {
		var renderTexture = preparedFrame.RenderView(device.ImmediateContext, hiddenAreaMesh, viewTransform, projectionTransform);
		
		mostRecentProjectionTransform = projectionTransform;
		mostRecentRenderTexture = renderTexture;
		
		return renderTexture;
	}

	private void SubmitEye(EVREye eye, IPreparedFrame preparedFrame) {
		immediateContext.WithEvent($"VRApp::SubmitEye({eye})", () => {
			HiddenAreaMesh hiddenAreaMesh = hiddenAreaMeshes.GetMesh(eye);
			Matrix viewMatrix = GetViewMatrix(eye);
			Matrix projectionMatrix = GetProjectionMatrix(eye);
			var resultTexture = RenderView(preparedFrame, hiddenAreaMesh, viewMatrix, projectionMatrix);

			VRTextureBounds_t bounds;
			bounds.uMin = 0;
			bounds.uMax = 1;
			bounds.vMin = 0;
			bounds.vMax = 1;
		
			Texture_t eyeTexture;
			eyeTexture.handle = resultTexture.NativePointer;
			eyeTexture.eType = ETextureType.DirectX;
			eyeTexture.eColorSpace = EColorSpace.Auto;
		
			OpenVR.Compositor.Submit(eye, ref eyeTexture, ref bounds, EVRSubmitFlags.Submit_Default);
		});
	}
	
	private void PumpVREvents() {
		VREvent_t vrEvent = default(VREvent_t);
		while (OpenVR.System.PollNextEvent(ref vrEvent)) {
			EVREventType type = (EVREventType) vrEvent.eventType;
			switch (type) {
				case EVREventType.VREvent_Quit:
					companionWindow.Form.Close();
					break;
				case EVREventType.VREvent_HideRenderModels:
					Debug.WriteLine("hide render models");
					break;
			}

			OpenVRKeyboardHelper.ProcessEvent(vrEvent);
		}
	}
	
	private void KickoffFramePreparation() {
		var headPosition = companionWindow.HasIndependentCamera ? companionWindow.CameraPosition : PlayerPositionUtils.GetHeadPosition(gamePoses);
		var updateParameters = new FrameUpdateParameters(
			timeKeeper.NextFrameTime, //need to go one frame ahead because this is for the next frame
			timeKeeper.TimeDelta,
			gamePoses,
			headPosition);
		asyncFramePreparer.StartPreparingFrame(updateParameters);
	}

	private void DoFrame() {
		OpenVR.Compositor.WaitGetPoses(poses, gamePoses);
		timeKeeper.AdvanceFrame();

		PumpVREvents();

		KickoffFramePreparation();
		
		immediateContext.WithEvent("VRApp::Prework", () => {
			preparedFrame.DoPrework(device.ImmediateContext, poses);
		});
		
		SubmitEye(EVREye.Eye_Left, preparedFrame);
		SubmitEye(EVREye.Eye_Right, preparedFrame);

		if (companionWindow.HasIndependentCamera) {
			Matrix companionViewTransform = companionWindow.GetViewTransform();
			Matrix companionWindowProjectionMatrix = companionWindow.GetDesiredProjectionMatrix();
			immediateContext.WithEvent("VRApp::RenderCompanionView", () => {
				RenderView(preparedFrame, null, companionViewTransform, companionWindowProjectionMatrix);
			});
		}

		companionWindow.Display(
			mostRecentRenderTexture,
			mostRecentProjectionTransform,
			() => preparedFrame.DrawCompanionWindowUi(device.ImmediateContext));
		
		preparedFrame.DoPostwork(device.ImmediateContext);

		OpenVR.Compositor.PostPresentHandoff();

		preparedFrame.Dispose();
		preparedFrame = asyncFramePreparer.FinishPreparingFrame();
	}
	
	private Matrix GetViewMatrix(EVREye eye) {
		var hmdPose = poses[OpenVR.k_unTrackedDeviceIndex_Hmd];
		var hmdTransform = hmdPose.mDeviceToAbsoluteTracking.Convert();
		hmdTransform.Invert();

		Matrix eyeTransform = OpenVR.System.GetEyeToHeadTransform(eye).Convert();
		eyeTransform.Invert();

		Matrix viewTransform = hmdTransform * eyeTransform;

		return viewTransform;
	}

	private Matrix GetProjectionMatrix(EVREye eye) {
		Matrix projection = OpenVR.System.GetProjectionMatrix(eye, RenderingConstants.ZNear, RenderingConstants.ZFar).Convert();
		return projection;
	}
}
