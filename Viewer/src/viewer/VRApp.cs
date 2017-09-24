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

	private static String GetStackTrace() {
		return new StackTrace(4, true).ToString();
	}

	[Conditional("DEBUG")]
	private static void SetupDebug() {
		Configuration.EnableObjectTracking = true;
		ObjectTracker.StackTraceProvider = GetStackTrace;

		HashSet<ComObject> trackedObjects = new HashSet<ComObject>();

		ObjectTracker.Tracked += (sender, eventArgs) => {
			trackedObjects.Add(eventArgs.Object);
		};
		ObjectTracker.UnTracked += (sender, eventArgs) => {
			trackedObjects.Remove(eventArgs.Object);
		};
	}

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

		var commandLineParser = new CommandLineApplication();
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
		
		SetupDebug();
		
		string title = Application.ProductName + " " + Application.ProductVersion;
		
		using (VRApp app = new VRApp(dataDir, title)) {
			app.Run();
		}
		
		if (ObjectTracker.FindActiveObjects().Count > 0) {
			Debug.WriteLine(ObjectTracker.ReportActiveObjects());
		} else {
			Debug.WriteLine("Zero leaked objects.");
		}
	}
		
	private readonly CompanionWindow companionWindow;

	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly DeviceContext deferredContext;
	private readonly DeviceContext immediateContext;
		
	private ViewProjectionConstantBufferManager viewProjectionTransformBufferManager;

	private OpenVRTimeKeeper timeKeeper;
	private TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	private TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	private ControllerManager controllerManager;

	private readonly HiddenAreaMasker hiddenAreaMasker;
	private RenderPassController passController;

	private readonly StandardSamplers standardSamplers;
	private readonly Scene scene;

	public VRApp(IArchiveDirectory dataDir, string title) {
		device = new Device(DriverType.Hardware, debugDevice ? DeviceCreationFlags.Debug : DeviceCreationFlags.None);
		shaderCache = new ShaderCache(device);
		standardSamplers = new StandardSamplers(device);
		
		companionWindow = new CompanionWindow(device, shaderCache, standardSamplers, title, dataDir);
				
		immediateContext = device.ImmediateContext;
		deferredContext = new DeviceContext(device);

		Init();
		
		var toneMappingMenuLevel = passController.RenderSettingsMenuLevel;
		scene = new Scene(dataDir, device, shaderCache, standardSamplers, poses, controllerManager, toneMappingMenuLevel);
		hiddenAreaMasker = new HiddenAreaMasker(device, shaderCache);
	}
	
	public void Dispose() {
		passController.Dispose();
		
		Utilities.Dispose(ref viewProjectionTransformBufferManager);

		scene.Dispose();
		standardSamplers.Dispose();
		hiddenAreaMasker.Dispose();

		OpenVR.Shutdown();

		deferredContext.Dispose();
		immediateContext.Dispose();
		
		companionWindow.Dispose();
		
		shaderCache.Dispose();
		device.Dispose();
	}

	private void Init() {
		viewProjectionTransformBufferManager = new ViewProjectionConstantBufferManager(device);
		
		InitVR();
	}

	public void InitVR() {
		OpenVRExtensions.Init();
		
		timeKeeper = new OpenVRTimeKeeper();
		controllerManager = new ControllerManager();

		Size2 targetSize = OpenVR.System.GetRecommendedRenderTargetSize();
		
		passController = new RenderPassController(device, shaderCache, targetSize);

		OpenVR.Compositor.GetLastPoses(poses, gamePoses);
	}
	
	private void Run() {
		RenderLoop.Run(companionWindow.Form, DoFrame);
	}
	
	private CommandList RecordCommandList() {
		DeviceContext context = deferredContext;

		standardSamplers.Apply(context.PixelShader);
		context.VertexShader.SetConstantBuffer(0, viewProjectionTransformBufferManager.Buffer);

		passController.RenderAllPases(context, pass => scene.RenderPass(context, pass));

		return context.FinishCommandList(false);
	}

	private void RenderView(CommandList commandList, Action<DeviceContext> prepareMaskAction, Matrix viewTransform, Matrix projectionTransform) {
		DeviceContext context = immediateContext;
		
		float c = ColorUtils.SrgbToLinear(68/255f);
		passController.Prepare(context, new Color(c, c, c), 255, () => prepareMaskAction.Invoke(context));

		viewProjectionTransformBufferManager.Update(immediateContext, viewTransform, projectionTransform);

		context.ExecuteCommandList(commandList, false);
	}

	private void SubmitEye(EVREye eye, CommandList commandList) {
		immediateContext.WithEvent($"VRApp::SubmitEye({eye})", () => {
			VRTextureBounds_t bounds;
			bounds.uMin = 0;
			bounds.uMax = 1;
			bounds.vMin = 0;
			bounds.vMax = 1;
		
			Texture_t eyeTexture;
			eyeTexture.handle = passController.ResultTexture.NativePointer;
			eyeTexture.eType = ETextureType.DirectX;
			eyeTexture.eColorSpace = EColorSpace.Auto;
		
			Action<DeviceContext> prepareMaskAction = (context) => hiddenAreaMasker.PrepareMask(context, eye);
			Matrix viewMatrix = GetViewMatrix(eye);
			Matrix projectionMatrix = GetProjectionMatrix(eye);
			RenderView(commandList, prepareMaskAction, viewMatrix, projectionMatrix);

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
		}
	}
	
	private void DoFrame() {
		var headPosition = companionWindow.HasIndependentCamera ? companionWindow.CameraPosition : PlayerPositionUtils.GetHeadPosition(gamePoses);
		var updateParameters = new FrameUpdateParameters(
			timeKeeper.GetNextFrameTime(1), //need to go one frame ahead because we haven't called WaitGetPoses yet
			timeKeeper.TimeDelta,
			headPosition);

		immediateContext.WithEvent("VRApp::Update", () => {
			controllerManager.Update();
			scene.Update(device.ImmediateContext, updateParameters);
			passController.PrepareFrame(device.ImmediateContext);
		});
		
		OpenVR.Compositor.WaitGetPoses(poses, gamePoses);
		timeKeeper.AdvanceFrame();

		Matrix companionWindowProjectionMatrix;

		using (var commandList = RecordCommandList()) {
			SubmitEye(EVREye.Eye_Left, commandList);
			SubmitEye(EVREye.Eye_Right, commandList);

			if (companionWindow.HasIndependentCamera) {
				Matrix companionViewTransform = companionWindow.GetViewTransform();
				companionWindowProjectionMatrix = companionWindow.GetDesiredProjectionMatrix();
				immediateContext.WithEvent("VRApp::RenderCompanionView", () => {
					RenderView(commandList, (context) => { }, companionViewTransform, companionWindowProjectionMatrix);
				});
			} else {
				companionWindowProjectionMatrix = GetProjectionMatrix(EVREye.Eye_Right);
			}
		}
		
		companionWindow.Display(
			passController.ResultSourceView,
			companionWindowProjectionMatrix,
			() => scene.RenderCompanionWindowUi(immediateContext));
		
		OpenVR.Compositor.PostPresentHandoff();

		PumpVREvents();
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
