using System;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX;
using Valve.VR;

public class MenuRenderer : IDisposable {
	private readonly TrackedDeviceBufferManager trackedDeviceBufferManager;
	private readonly ControllerManager controllerManager;
	private readonly ShaderResourceView menuViewTexture;
	private readonly VertexShader vertexShader;
	private readonly VertexShader companionWindowVertexShader;
	private readonly PixelShader pixelShader;

	private readonly CoordinateNormalMatrixPairConstantBufferManager menuToControllerSpaceTransform;

	public MenuRenderer(Device device, ShaderCache shaderCache, TrackedDeviceBufferManager trackedDeviceBufferManager, ControllerManager controllerManager, ShaderResourceView menuViewTexture) {
		this.trackedDeviceBufferManager = trackedDeviceBufferManager;
		this.controllerManager = controllerManager;
		this.menuViewTexture = menuViewTexture;
		vertexShader = shaderCache.GetVertexShader<MenuRenderer>("menu/renderer/Menu");
		companionWindowVertexShader = shaderCache.GetVertexShader<MenuRenderer>("menu/renderer/CompanionWindowMenu");
		pixelShader = shaderCache.GetPixelShader<MenuRenderer>("menu/renderer/Menu");
		menuToControllerSpaceTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);

		Matrix menuToControllerTransform = Matrix.Scaling(0.10f) * Matrix.Translation(0, 0, 0f) * Matrix.RotationX(MathUtil.PiOverTwo);
		menuToControllerSpaceTransform.Update(device.ImmediateContext, menuToControllerTransform);
	}

	public void Dispose() {
		menuToControllerSpaceTransform.Dispose();
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
		context.VertexShader.SetConstantBuffer(2, menuToControllerSpaceTransform.Buffer);

		context.PixelShader.Set(pixelShader);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, menuViewTexture);

		for (uint deviceIdx = 0; deviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++deviceIdx) {
			if (!controllerManager.StateTrackers[deviceIdx].MenuActive) {
				continue;
			}
			
			context.VertexShader.SetConstantBuffer(1, trackedDeviceBufferManager.GetObjectToWorldSpaceTransformBuffer(deviceIdx));
			
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
