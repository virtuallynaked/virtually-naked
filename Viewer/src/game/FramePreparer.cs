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
	private readonly ControllerManager controllerManager;
	private readonly Scene scene;

	public FramePreparer(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, StandardSamplers standardSamplers, Size2 targetSize, TrackedDevicePose_t[] poses) {
		this.standardSamplers = standardSamplers;

		deferredContext = new DeviceContext(device);
		viewProjectionTransformBufferManager = new ViewProjectionConstantBufferManager(device);
		passController = new RenderPassController(device, shaderCache, targetSize);
		masker = new HiddenAreaMasker(device, shaderCache);
		controllerManager = new ControllerManager();
		scene = new Scene(dataDir, device, shaderCache, standardSamplers, poses, controllerManager);
	}

	public void Dispose() {
		scene.Dispose();

		deferredContext.Dispose();
		passController.Dispose();
		masker.Dispose();
		viewProjectionTransformBufferManager.Dispose();
	}

	private CommandList UpdateAndRecordUpdateCommandList(FrameUpdateParameters updateParameters) {
		DeviceContext context = deferredContext;

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

	private CommandList RecordDrawCommandUiCommandList() {
		DeviceContext context = deferredContext;

		scene.RenderCompanionWindowUi(context);

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

	private void DoPostwork(DeviceContext deviceContext) {
		scene.DoPostwork(deviceContext);
	}

	public IPreparedFrame PrepareFrame(FrameUpdateParameters updateParameters) {
		return new PreparedFrame(
			UpdateAndRecordUpdateCommandList(updateParameters),

			PrepareView,
			RecordDrawCommandList(),
			passController.ResultTexture,

			RecordDrawCommandUiCommandList(),

			DoPostwork
		);
	}
}

public class PreparedFrame : IPreparedFrame {
	private CommandList updateCommandList;

	private Action<DeviceContext, HiddenAreaMesh, Matrix, Matrix> prepareViewAction;
	private CommandList drawViewCommandList;
	private Texture2D renderTexture;

	private CommandList drawCompanionWindowUiCommandList;

	private Action<DeviceContext> postworkAction;

	public PreparedFrame(
		CommandList updateCommandList,
		Action<DeviceContext, HiddenAreaMesh, Matrix, Matrix> prepareViewAction, CommandList drawViewCommandList, Texture2D renderTexture,
		CommandList drawCompanionWindowUiCommandList,
		Action<DeviceContext> postworkAction) {
		this.updateCommandList = updateCommandList;

		this.prepareViewAction = prepareViewAction;
		this.drawViewCommandList = drawViewCommandList;
		this.renderTexture = renderTexture;

		this.drawCompanionWindowUiCommandList = drawCompanionWindowUiCommandList;

		this.postworkAction = postworkAction;
	}

	public void Dispose() {
		updateCommandList.Dispose();
		drawViewCommandList.Dispose();
		drawCompanionWindowUiCommandList.Dispose();
	}

	public void DoPrework(DeviceContext context) {
		context.ExecuteCommandList(updateCommandList, false);
	}
		
	public Texture2D RenderView(DeviceContext context, HiddenAreaMesh hiddenAreaMesh, Matrix viewTransform, Matrix projectionTransform) {
		prepareViewAction(context, hiddenAreaMesh, viewTransform, projectionTransform);
		context.ExecuteCommandList(drawViewCommandList, false);
		return renderTexture;
	}

	public void DrawCompanionWindowUi(DeviceContext context) {
		context.ExecuteCommandList(drawCompanionWindowUiCommandList, false);
	}

	public void DoPostwork(DeviceContext context) {
		postworkAction(context);
	}
}