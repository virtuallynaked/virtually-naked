using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Valve.VR;
using Buffer = SharpDX.Direct3D11.Buffer;

class RenderModelCache : IDisposable {
	private static readonly int VertexDataSize = Marshal.SizeOf<RenderModel_Vertex_t>();

	class Definition : IDisposable {
		public VertexBufferBinding vertexBufferBinding;
		public Buffer indexBuffer;
		public int indexCount;
		public int textureId;

		public void Dispose() {
			vertexBufferBinding.Buffer.Dispose();
			indexBuffer.Dispose();
		}
	}

	private readonly Device device;
	private readonly BasicSpecularMaterial.Factory materialFactory;

	private readonly Dictionary<string, Definition> definitionCache = new Dictionary<string, Definition>();
	private readonly Dictionary<int, IOpaqueMaterial> materialCache = new Dictionary<int, IOpaqueMaterial>();
	
	private readonly Dictionary<string, RenderModel> modelCache = new Dictionary<string, RenderModel>();
	private readonly Dictionary<string, RenderModelComponent[]> componentsCache = new Dictionary<string, RenderModelComponent[]>();

	public RenderModelCache(Device device, BasicSpecularMaterial.Factory materialFactory) {
		this.device = device;
		this.materialFactory = materialFactory;
	}

	public void Dispose() {
		foreach (var item in definitionCache.Values) {
			item.Dispose();
		}
		foreach (var item in materialCache.Values) {
			item.Dispose();
		}
	}

	public RenderModel LookupModel(DeviceContext context, string name) {
		if (modelCache.TryGetValue(name, out RenderModel model)) {
			return model;
		}

		if (!definitionCache.TryGetValue(name, out Definition definition)) {
			definition = TryLoadDefinition(name);
			if (definition == null) {
				return null;
			}
			definitionCache.Add(name, definition);
		}

		int textureId = definition.textureId;
		if (!materialCache.TryGetValue(textureId, out IOpaqueMaterial material)) {
			var textureView = TryLoadTexture(context, textureId);
			if (textureView == null) {
				return null;
			}
			material = materialFactory.Make(textureView);
			materialCache.Add(textureId, material);
		}
		
		model = new RenderModel(
			definition.vertexBufferBinding,
			definition.indexBuffer,
			definition.indexCount,
			material);

		modelCache.Add(name, model);

		return model;
	}

	public RenderModelComponent[] LookupComponents(DeviceContext context, string renderModelName) {
		if (componentsCache.TryGetValue(renderModelName, out RenderModelComponent[] components)) {
			return components;
		}

		uint componentCount = OpenVR.RenderModels.GetComponentCount(renderModelName);
		components = new RenderModelComponent[componentCount];
		
		for (uint componentIdx = 0; componentIdx < componentCount; ++componentIdx) {
			var name = OpenVR.RenderModels.GetComponentName(renderModelName, componentIdx);

			RenderModel model;
			var componentModelName = OpenVR.RenderModels.GetComponentRenderModelName(renderModelName, name);
			if (componentModelName != null) {
				model = LookupModel(context, componentModelName);

				if (model == null) {
					return null;
				}
			} else {
				//this is a non-visual component
				model = null;
			}

			components[componentIdx] = new RenderModelComponent(name, model);
		}
		
		componentsCache.Add(renderModelName, components);

		return components;
	}

	private Definition TryLoadDefinition(string name) {
		IntPtr pDefinition = IntPtr.Zero;
		var errorCode = OpenVR.RenderModels.LoadRenderModel_Async(name, ref pDefinition);
		if (errorCode == EVRRenderModelError.Loading) {
			return null;
		}
		if (errorCode != EVRRenderModelError.None) {
			throw OpenVRException.Make(errorCode);
		}
		var rawDefinition = Marshal.PtrToStructure<RenderModel_t>(pDefinition);

		int indexCount = (int) rawDefinition.unTriangleCount * 3;

		var vertexBuffer = new SharpDX.Direct3D11.Buffer(device, rawDefinition.rVertexData, new BufferDescription() {
			SizeInBytes = (int) rawDefinition.unVertexCount * VertexDataSize,
			BindFlags = BindFlags.VertexBuffer,
			Usage = ResourceUsage.Immutable
		});

		var definition = new Definition() {
			indexCount = (int) indexCount,
			vertexBufferBinding = new VertexBufferBinding(vertexBuffer, VertexDataSize, 0),
			indexBuffer = new SharpDX.Direct3D11.Buffer(device, rawDefinition.rIndexData, new BufferDescription() {
				SizeInBytes = indexCount * sizeof(ushort),
				BindFlags = BindFlags.IndexBuffer,
				Usage = ResourceUsage.Immutable
			}),
			textureId = rawDefinition.diffuseTextureId
		};

		OpenVR.RenderModels.FreeRenderModel(pDefinition);

		return definition;
	}

	private ShaderResourceView TryLoadTexture(DeviceContext context, int textureId) {
		IntPtr pTexture = IntPtr.Zero;
		var errorCode = OpenVR.RenderModels.LoadTextureD3D11_Async(textureId, device.NativePointer, ref pTexture);
		if (errorCode == EVRRenderModelError.Loading) {
			return null;
		}
		if (errorCode != EVRRenderModelError.None) {
			throw OpenVRException.Make(errorCode);
		}

		ShaderResourceView textureView;
		Texture2DDescription textureDescription;
		using (Texture2D texture = new Texture2D(pTexture)) {
			// Need to do an AddRef because SharpDX from-IntPtr constructors don't do one automatically.
			// I can't just skip the Dispose either because the SharpDX leak detector maintains its own reference count.
			((IUnknown) texture).AddReference();

			textureDescription = texture.Description;
			textureView = new ShaderResourceView(device, texture);
		}

		OpenVR.RenderModels.FreeTextureD3D11(pTexture);
		
		if (textureDescription.OptionFlags.HasFlag(ResourceOptionFlags.GenerateMipMaps)) {
			context.GenerateMips(textureView);
		}

		return textureView;
	}
}