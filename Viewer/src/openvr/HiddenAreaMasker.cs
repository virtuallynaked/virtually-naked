using SharpDX.Direct3D11;
using System;

class HiddenAreaMasker : IDisposable {
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly PixelShader pixelShader;
	private readonly DepthStencilState writeStencilState;
	private readonly RasterizerState noCullRasterizerState;

	public HiddenAreaMasker(Device device, ShaderCache shaderCache) {
		var vertexShaderAndBytecode = shaderCache.GetVertexShader<HiddenAreaMasker>("openvr/HiddenAreaVertexShader");
		vertexShader = vertexShaderAndBytecode;
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, HiddenAreaMesh.InputElements);

		pixelShader = shaderCache.GetPixelShader<HiddenAreaMasker>("openvr/BlackPixelShader");

		writeStencilState = MakeWriteStencilState(device);
		noCullRasterizerState = MakeNoCullRasterizerState(device);
	}

	public void Dispose() {
		inputLayout.Dispose();
		writeStencilState.Dispose();
		noCullRasterizerState.Dispose();
	}

	private DepthStencilState MakeWriteStencilState(Device device) {
		DepthStencilStateDescription desc = DepthStencilStateDescription.Default();
		desc.DepthComparison = Comparison.Greater;
		desc.IsStencilEnabled = true;
		desc.FrontFace.PassOperation = StencilOperation.Zero;
		desc.BackFace.PassOperation = StencilOperation.Zero;
		return new DepthStencilState(device, desc);
	}
	
	private RasterizerState MakeNoCullRasterizerState(Device device) {
		RasterizerStateDescription desc = RasterizerStateDescription.Default();
		desc.CullMode = CullMode.None;
		return new RasterizerState(device, desc);
	}

	public void PrepareMask(DeviceContext context, HiddenAreaMesh mesh) {
		context.Rasterizer.State = noCullRasterizerState;
		context.OutputMerger.SetDepthStencilState(writeStencilState);

		context.InputAssembler.InputLayout = inputLayout;
		context.VertexShader.Set(vertexShader);
		context.PixelShader.Set(pixelShader);
		
		mesh?.Draw(context);
	}
}
