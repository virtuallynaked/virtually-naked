using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

class HiddenAreaMasker : IDisposable {
	private readonly HiddenAreaMesh leftEyeMesh;
	private readonly HiddenAreaMesh rightEyeMesh;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly PixelShader pixelShader;
	private readonly DepthStencilState writeStencilState;
	private readonly RasterizerState noCullRasterizerState;

	public HiddenAreaMasker(Device device, ShaderCache shaderCache) {
		leftEyeMesh = HiddenAreaMesh.Make(device, EVREye.Eye_Left);
		rightEyeMesh = HiddenAreaMesh.Make(device, EVREye.Eye_Right);
		
		var vertexShaderAndBytecode = shaderCache.GetVertexShader<HiddenAreaMasker>("viewer/HiddenAreaVertexShader");
		vertexShader = vertexShaderAndBytecode;
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, HiddenAreaMesh.InputElements);

		pixelShader = shaderCache.GetPixelShader<HiddenAreaMasker>("viewer/BlackPixelShader");

		writeStencilState = MakeWriteStencilState(device);
		noCullRasterizerState = MakeNoCullRasterizerState(device);
	}

	public void Dispose() {
		leftEyeMesh?.Dispose();
		rightEyeMesh?.Dispose();
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

	public void PrepareMask(DeviceContext context, EVREye eye) {
		context.Rasterizer.State = noCullRasterizerState;
		context.OutputMerger.SetDepthStencilState(writeStencilState);

		context.InputAssembler.InputLayout = inputLayout;
		context.VertexShader.Set(vertexShader);
		context.PixelShader.Set(pixelShader);

		HiddenAreaMesh mesh = eye == EVREye.Eye_Left ? leftEyeMesh : rightEyeMesh;
		mesh?.Draw(context);
	}
}
