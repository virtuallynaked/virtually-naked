using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Device3 = SharpDX.Direct3D11.Device3;

public class TextureMaskRenderer : IDisposable {
	private static InputElement[] InputElements = new[] {
		new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
	};

	private readonly Device3 device;

	private readonly InputLayout inputLayout;
	private readonly VertexShader vertexShader;
	private readonly RasterizerState2 rasterizerState;
	private readonly PixelShader pixelShader;
	private readonly BlendState blendState;

	public TextureMaskRenderer(Device device, ShaderCache shaderCache) {
		this.device = device.QueryInterface<Device3>();
		
		var features = this.device.CheckD3D113Features2();

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<TextureMaskRenderer>("texturing/processing/TextureMask");
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, QuadMeshBuffers.InputElements);
		vertexShader = vertexShaderAndBytecode;

		var rasterizerStateDesc = new RasterizerStateDescription2() {
			FillMode = FillMode.Solid,
			CullMode = CullMode.None,
			IsFrontCounterClockwise = false,
			DepthBias = 0,
			SlopeScaledDepthBias = 0.0f,
			DepthBiasClamp = 0.0f,
			IsDepthClipEnabled = true,
			IsScissorEnabled = false,
			IsMultisampleEnabled = true,
			IsAntialiasedLineEnabled = false,
			ConservativeRasterizationMode = ConservativeRasterizationMode.On
		};
		rasterizerState = new RasterizerState2(this.device, rasterizerStateDesc); 
		
		pixelShader = shaderCache.GetPixelShader<TextureMaskRenderer>("texturing/processing/TextureMask");

		var blendStateDesc = BlendStateDescription.Default();
		blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Alpha;
		blendState = new BlendState(device, blendStateDesc);
	}
	
	public void Dispose() {
		inputLayout.Dispose();
		rasterizerState.Dispose();
		blendState.Dispose();
	}

	public void RenderMaskToAlpha(TextureMask mask, Size2 size, Texture2D destTexture) {
		var vertices = mask.GetMaskVertices();

		// Not sure by, possibly a nvidia bug, but the vertex buffer padding by an extra element is necessary.
		// Otherwise (0,0) gets passed to the vertex shader instead of the last vertex.
		var vertexBufferSizeInBytes = Vector2.SizeInBytes * (vertices.Length + 1); 
		var vertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertices, vertexBufferSizeInBytes);
		
		var indices = mask.GetMaskTriangleIndices();
		var indexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices.ToArray());
		
		var renderTargetView = new RenderTargetView(device, destTexture);
		
		var context = device.ImmediateContext;
		
		context.ClearState();
		
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		context.InputAssembler.InputLayout = inputLayout;
		context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Vector2.SizeInBytes, 0));
		context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);

		context.VertexShader.Set(vertexShader);

		context.Rasterizer.SetViewport(0, 0, size.Width, size.Height);
		context.Rasterizer.State = rasterizerState;

		context.PixelShader.Set(pixelShader);

		context.OutputMerger.SetBlendState(blendState);
		context.OutputMerger.SetRenderTargets(renderTargetView);

		context.DrawIndexed(indices.Count, 0, 0);
				
		context.ClearState();

		vertexBuffer.Dispose();
		indexBuffer.Dispose();
		renderTargetView.Dispose();
	}
}
