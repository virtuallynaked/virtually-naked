using System;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX;
using Valve.VR;

public class MenuRenderer : IDisposable {
	private readonly ControllerManager controllerManager;
	private readonly ShaderResourceView menuViewTexture;
	private readonly VertexShader vertexShader;
	private readonly VertexShader companionWindowVertexShader;
	private readonly PixelShader pixelShader;

	private readonly CoordinateNormalMatrixPairConstantBufferManager objectToWorldTransform;

	public MenuRenderer(Device device, ShaderCache shaderCache, ControllerManager controllerManager, ShaderResourceView menuViewTexture) {
		this.controllerManager = controllerManager;
		this.menuViewTexture = menuViewTexture;
		vertexShader = shaderCache.GetVertexShader<MenuRenderer>("menu/renderer/Menu");
		companionWindowVertexShader = shaderCache.GetVertexShader<MenuRenderer>("menu/renderer/CompanionWindowMenu");
		pixelShader = shaderCache.GetPixelShader<MenuRenderer>("menu/renderer/Menu");
		objectToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);
	}

	public void Dispose() {
		objectToWorldTransform.Dispose();
	}

	public volatile bool anyMenuActive = false;

	public void Update() {
		anyMenuActive = controllerManager.AnyMenuActive;
	}
		
	public void RenderPass(DeviceContext context, RenderingPass pass) {
		if (pass.Layer != RenderingLayer.UiElements) {
			return;
		}

		if (pass.OutputMode != OutputMode.Standard) {
			throw new InvalidOperationException();
		}
		
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		
		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(1, objectToWorldTransform.Buffer);

		context.PixelShader.Set(pixelShader);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, menuViewTexture);

		for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
			if (!controllerManager.StateTrackers[deviceIdx].MenuActive) {
				continue;
			}

			TrackedDevicePose_t pose = default(TrackedDevicePose_t);
			TrackedDevicePose_t gamePose = default(TrackedDevicePose_t);
			OpenVR.Compositor.GetLastPoseForTrackedDeviceIndex(deviceIdx, ref pose, ref gamePose);
			Matrix controllerToWorldTransform = pose.mDeviceToAbsoluteTracking.Convert();
			Matrix menuToControllerTransform = Matrix.Scaling(0.10f) * Matrix.Translation(0, 0, 0f) * Matrix.RotationX(MathUtil.PiOverTwo);
			objectToWorldTransform.Update(context, menuToControllerTransform * controllerToWorldTransform);
			
			context.Draw(2 * 20, 0);
		}
	}
	
	public void DoDrawCompanionWindowUi(DeviceContext context) {
		if (!anyMenuActive) {
			return;
		}

		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		
		context.VertexShader.Set(companionWindowVertexShader);

		context.PixelShader.Set(pixelShader);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, menuViewTexture);

		context.Draw(4, 0);
	}
}
