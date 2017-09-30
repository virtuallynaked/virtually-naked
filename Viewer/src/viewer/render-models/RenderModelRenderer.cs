using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using System;
using System.Collections.Generic;
using Valve.VR;
using SharpDX;

class RenderModelRenderer : IDisposable {
	private static readonly InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
		new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
		new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0)
	};

	private readonly RenderModelCache cache;
	private readonly TrackedDevicePose_t[] poses;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly CoordinateNormalMatrixPairConstantBufferManager worldTransformBufferManager;
	
	public RenderModelRenderer(Device device, ShaderCache shaderCache, TrackedDevicePose_t[] poses) {
		this.cache = new RenderModelCache(device, new BasicSpecularMaterial.Factory(device, shaderCache));
		this.poses = poses;

		var vertexShaderWithBytecode = shaderCache.GetVertexShader<RenderModelRenderer>("viewer/render-models/RenderModelVertex");
		vertexShader = vertexShaderWithBytecode;
		inputLayout = new InputLayout(device, vertexShaderWithBytecode.Bytecode, InputElements);
		
		this.worldTransformBufferManager = new CoordinateNormalMatrixPairConstantBufferManager(device);
	}
	
	public void Dispose() {
		worldTransformBufferManager.Dispose();
		cache.Dispose();
		inputLayout.Dispose();
	}
	
	public void Render(DeviceContext context) {
		context.InputAssembler.InputLayout = inputLayout;
		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(1, worldTransformBufferManager.Buffer);

		for (uint trackedDeviceIdx = 0; trackedDeviceIdx < poses.Length; ++trackedDeviceIdx) {
			var pose = poses[trackedDeviceIdx];
			if (!pose.bPoseIsValid) {
				continue;
			}

			var deviceClass = OpenVR.System.GetTrackedDeviceClass(trackedDeviceIdx);
			if (deviceClass != ETrackedDeviceClass.Controller) {
				continue;
			}
			
			Matrix worldMatrix = pose.mDeviceToAbsoluteTracking.Convert();
			
			string renderModelName = OpenVR.System.GetStringTrackedDeviceProperty(trackedDeviceIdx, ETrackedDeviceProperty.Prop_RenderModelName_String);
			uint componentCount = OpenVR.RenderModels.GetComponentCount(renderModelName);

			if (componentCount == 0) {
				RenderSingleComponentModel(context, worldMatrix, renderModelName);
			} else {
				RenderMultiComponentModel(context, worldMatrix, trackedDeviceIdx, renderModelName);
			}
		}
	}

	private void RenderSingleComponentModel(DeviceContext context, Matrix worldMatrix, string renderModelName) {
		var model = cache.LookupModel(context, renderModelName);
		if (model != null) {
			worldTransformBufferManager.Update(context, worldMatrix);
			model.Render(context);
		}
	}

	private void RenderMultiComponentModel(DeviceContext context, Matrix worldMatrix, uint trackedDeviceIdx, string renderModelName) {
		var components = cache.LookupComponents(context, renderModelName);
		if (components == null) {
			return;
		}
		
		OpenVR.System.GetControllerState(trackedDeviceIdx, out VRControllerState_t controllerState);

		RenderModel_ControllerMode_State_t controllerMode = new RenderModel_ControllerMode_State_t {
			bScrollWheelVisible = false
		};

		for (uint componentIdx = 0; componentIdx < components.Length; ++componentIdx) {
			var component = components[componentIdx];
			
			if (component.model == null) {
				//this is a non-visual component
				continue;
			}

			RenderModel_ComponentState_t componentState = default(RenderModel_ComponentState_t);
			OpenVR.RenderModels.GetComponentState(renderModelName, component.name, ref controllerState, ref controllerMode, ref componentState);
			
			bool isVisible = ((EVRComponentProperty) componentState.uProperties).HasFlag(EVRComponentProperty.IsVisible);
			if (isVisible) {
				Matrix componentWorldMatrix = componentState.mTrackingToComponentRenderModel.Convert();
				componentWorldMatrix *= worldMatrix;
				worldTransformBufferManager.Update(context, componentWorldMatrix);
				
				component.model.Render(context);
			}
		}
	}
}