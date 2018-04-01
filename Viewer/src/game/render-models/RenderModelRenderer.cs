using SharpDX.Direct3D11;
using Format = SharpDX.DXGI.Format;
using System;
using Valve.VR;
using SharpDX;

class RenderModelRenderer : IDisposable {
	private static readonly InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
		new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
		new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0)
	};

	private readonly TrackedDeviceBufferManager trackedDeviceBufferManager;
	private readonly RenderModelCache cache;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly CoordinateNormalMatrixPairConstantBufferManager componentSpaceToObjectTransformBufferManager;
	private readonly bool[] isPoseValid = new bool[OpenVR.k_unMaxTrackedDeviceCount];
	
	public RenderModelRenderer(Device device, ShaderCache shaderCache, TrackedDeviceBufferManager trackedDeviceBufferManager) {
		this.trackedDeviceBufferManager = trackedDeviceBufferManager;

		this.cache = new RenderModelCache(device, new BasicSpecularMaterial.Factory(device, shaderCache));

		var vertexShaderWithBytecode = shaderCache.GetVertexShader<RenderModelRenderer>("game/render-models/RenderModelVertex");
		vertexShader = vertexShaderWithBytecode;
		inputLayout = new InputLayout(device, vertexShaderWithBytecode.Bytecode, InputElements);
		
		this.componentSpaceToObjectTransformBufferManager = new CoordinateNormalMatrixPairConstantBufferManager(device);
	}
	
	public void Dispose() {
		componentSpaceToObjectTransformBufferManager.Dispose();
		cache.Dispose();
		inputLayout.Dispose();
	}

	public void Update(FrameUpdateParameters updateParameters) {
		for (uint trackedDeviceIdx = 0; trackedDeviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++trackedDeviceIdx) {
			isPoseValid[trackedDeviceIdx] = updateParameters.GamePoses[trackedDeviceIdx].bPoseIsValid;
		}
	}
	
	public void Render(DeviceContext context, bool depthOnly) {
		context.InputAssembler.InputLayout = inputLayout;
		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(2, componentSpaceToObjectTransformBufferManager.Buffer);

		for (uint trackedDeviceIdx = 0; trackedDeviceIdx < OpenVR.k_unMaxTrackedDeviceCount; ++trackedDeviceIdx) {
			if (!isPoseValid[trackedDeviceIdx]) {
				continue;
			}

			var deviceClass = OpenVR.System.GetTrackedDeviceClass(trackedDeviceIdx);
			if (deviceClass != ETrackedDeviceClass.Controller) {
				continue;
			}
			
			context.VertexShader.SetConstantBuffer(1, trackedDeviceBufferManager.GetObjectToWorldSpaceTransformBuffer(trackedDeviceIdx));
			
			string renderModelName = OpenVR.System.GetStringTrackedDeviceProperty(trackedDeviceIdx, ETrackedDeviceProperty.Prop_RenderModelName_String);
			uint componentCount = OpenVR.RenderModels.GetComponentCount(renderModelName);
			
			if (componentCount == 0) {
				RenderSingleComponentModel(context, depthOnly, renderModelName);
			} else {
				RenderMultiComponentModel(context, depthOnly, trackedDeviceIdx, renderModelName);
			}
		}
	}

	private void RenderSingleComponentModel(DeviceContext context, bool depthOnly, string renderModelName) {
		var model = cache.LookupModel(context, renderModelName);
		if (model != null) {
			componentSpaceToObjectTransformBufferManager.Update(context, Matrix.Identity);
			model.Render(context, depthOnly);
		}
	}

	private void RenderMultiComponentModel(DeviceContext context, bool depthOnly, uint trackedDeviceIdx, string renderModelName) {
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
				Matrix componentToObjectMatrix = componentState.mTrackingToComponentRenderModel.Convert();
				componentSpaceToObjectTransformBufferManager.Update(context, componentToObjectMatrix);
				component.model.Render(context, depthOnly);
			}
		}
	}
}
