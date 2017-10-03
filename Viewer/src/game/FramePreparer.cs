using SharpDX;
using SharpDX.Direct3D11;
using System;
using Valve.VR;

public class FramePreparer : IDisposable {
	private readonly StandardSamplers standardSamplers;

	private readonly DeviceContext deferredContext;
	private RenderPassController passController;
	private HiddenAreaMasker masker;
	private ViewProjectionConstantBufferManager viewProjectionTransformBufferManager;
	private readonly TrackedDeviceBufferManager trackedDeviceBufferManager;
	private readonly ControllerManager controllerManager;
	private readonly Scene scene;

	public FramePreparer(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, StandardSamplers standardSamplers, Size2 targetSize) {
		this.standardSamplers = standardSamplers;

		deferredContext = new DeviceContext(device);
		viewProjectionTransformBufferManager = new ViewProjectionConstantBufferManager(device);
		passController = new RenderPassController(device, shaderCache, targetSize);
		masker = new HiddenAreaMasker(device, shaderCache);
		trackedDeviceBufferManager = new TrackedDeviceBufferManager(device);
		controllerManager = new ControllerManager();
		scene = new Scene(dataDir, device, shaderCache, standardSamplers, trackedDeviceBufferManager, controllerManager);
	}

	public void Dispose() {
		scene.Dispose();
		trackedDeviceBufferManager.Dispose();

		deferredContext.Dispose();
		passController.Dispose();
		masker.Dispose();
		viewProjectionTransformBufferManager.Dispose();
	}

	private CommandList UpdateAndRecordUpdateCommandList(FrameUpdateParameters updateParameters) {
		DeviceContext context = deferredContext;

		trackedDeviceBufferManager.Update(updateParameters);
		controllerManager.Update();
		scene.Update(deferredContext, updateParameters);
		passController.PrepareFrame(deferredContext, scene.ToneMappingSettings);

		return context.FinishCommandList(false);
	}

	private CommandList RecordDrawCommandList() {
		DeviceContext context = deferredContext;

		standardSamplers.Apply(context.PixelShader);
		context.VertexShader.SetConstantBuffer(0, viewProjectionTransformBufferManager.Buffer);

		passController.RenderAllPases(context, pass => scene.RenderPass(context, pass));

		return context.FinishCommandList(false);
	}
	
	private void PrepareView(DeviceContext context, HiddenAreaMesh hiddenAreaMesh, Matrix viewTransform, Matrix projectionTransform) {
		float c = ColorUtils.SrgbToLinear(68/255f);

		Action prepareMask = () => {
			masker.PrepareMask(context, hiddenAreaMesh);
		};

		passController.Prepare(context, new Color(c, c, c), 255, prepareMask);

		viewProjectionTransformBufferManager.Update(context, viewTransform, projectionTransform);
	}

	private void DoPrework(DeviceContext context) {
		scene.DoPrework(context);
	}

	private void DoDrawCompanionWindowUi(DeviceContext context) {
		scene.DoDrawCompanionWindowUi(context);
	}

	private void DoPostwork(DeviceContext context) {
		scene.DoPostwork(context);
	}


	public IPreparedFrame PrepareFrame(FrameUpdateParameters updateParameters) {
		return new PreparedFrame(
			DoPrework,
			UpdateAndRecordUpdateCommandList(updateParameters),

			PrepareView,
			RecordDrawCommandList(),
			passController.ResultTexture,

			DoDrawCompanionWindowUi,

			DoPostwork
		);
	}
}

public class PreparedFrame : IPreparedFrame {
	private Action<DeviceContext> preworkAction;
	private CommandList updateCommandList;

	private Action<DeviceContext, HiddenAreaMesh, Matrix, Matrix> prepareViewAction;
	private CommandList drawViewCommandList;
	private Texture2D renderTexture;

	private Action<DeviceContext> drawCompanionWindowUiAction;

	private Action<DeviceContext> postworkAction;

	public PreparedFrame(
		Action<DeviceContext> preworkAction, CommandList updateCommandList,
		Action<DeviceContext, HiddenAreaMesh, Matrix, Matrix> prepareViewAction, CommandList drawViewCommandList, Texture2D renderTexture,
		Action<DeviceContext> drawCompanionWindowUiAction,
		Action<DeviceContext> postworkAction) {
		this.preworkAction = preworkAction;
		this.updateCommandList = updateCommandList;

		this.prepareViewAction = prepareViewAction;
		this.drawViewCommandList = drawViewCommandList;
		this.renderTexture = renderTexture;

		this.drawCompanionWindowUiAction = drawCompanionWindowUiAction;

		this.postworkAction = postworkAction;
	}

	public void Dispose() {
		updateCommandList.Dispose();
		drawViewCommandList.Dispose();
	}

	public void DoPrework(DeviceContext context) {
		preworkAction.Invoke(context);
		context.ExecuteCommandList(updateCommandList, false);
	}
		
	public Texture2D RenderView(DeviceContext context, HiddenAreaMesh hiddenAreaMesh, Matrix viewTransform, Matrix projectionTransform) {
		prepareViewAction(context, hiddenAreaMesh, viewTransform, projectionTransform);
		context.ExecuteCommandList(drawViewCommandList, false);
		return renderTexture;
	}

	public void DrawCompanionWindowUi(DeviceContext context) {
		drawCompanionWindowUiAction.Invoke(context);
	}

	public void DoPostwork(DeviceContext context) {
		postworkAction(context);
	}
}