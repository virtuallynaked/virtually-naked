using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using Device = SharpDX.Direct3D11.Device;
using System;

public class FigureRenderer : IDisposable {
	private readonly Scatterer scatterer;
	private readonly VertexRefiner vertexRefiner;
	private readonly MaterialSet materialSet;
	private readonly FigureSurface[] surfaces;

	private readonly bool isOneSided;
	private readonly int[] surfaceOrder;
	private readonly bool[] areUnorderedTransparent;

	private readonly VertexShader vertexShader;
	private readonly VertexShader falseDepthVertexShader;
	private readonly InputLayout inputLayout;

	public FigureRenderer(
		Device device, ShaderCache shaderCache, Scatterer scatterer, VertexRefiner vertexRefiner, MaterialSet materialSet, FigureSurface[] surfaces,
		bool isOneSided, int[] surfaceOrder, bool[] areUnorderedTranparent) {
		this.scatterer = scatterer;
		this.vertexRefiner = vertexRefiner;
		this.materialSet = materialSet;
		this.surfaces = surfaces;
		
		this.isOneSided = isOneSided;
		this.surfaceOrder = surfaceOrder;
		this.areUnorderedTransparent = areUnorderedTranparent;
		
		var vertexShaderAndBytecode = shaderCache.GetVertexShader<FigureRenderer>("figure/rendering/Figure");
		this.vertexShader = vertexShaderAndBytecode;
		this.inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, vertexRefiner.RefinedVertexBufferInputElements);
		falseDepthVertexShader = shaderCache.GetVertexShader<FigureRenderer>("figure/rendering/Figure-FalseDepth");
	}
		
	public void Dispose() {
		scatterer?.Dispose();
		vertexRefiner.Dispose();
		foreach (var surface in surfaces) {
			surface.Dispose();
		}
		materialSet.Dispose();
		inputLayout.Dispose();
	}
		
	public void Update(DeviceContext context, ImageBasedLightingEnvironment lightingEnvironment, ShaderResourceView controlVertexInfosView) {
		scatterer?.Scatter(context, lightingEnvironment, controlVertexInfosView);
		vertexRefiner.RefineVertices(context, controlVertexInfosView, scatterer?.ScatteredIlluminationView);
	}
	
	public void RenderPass(DeviceContext context, RenderingPass pass) {
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        context.InputAssembler.SetVertexBuffers(0, vertexRefiner.RefinedVertexBufferBinding);
		context.InputAssembler.InputLayout = inputLayout;

		var vertexShaderForMode = pass.OutputMode == OutputMode.FalseDepth ? falseDepthVertexShader : vertexShader;
		context.VertexShader.Set(vertexShaderForMode);
		
		foreach (int surfaceIdx in surfaceOrder) {
			var surface = surfaces[surfaceIdx];
			var material = materialSet.Materials[surfaceIdx];
			bool isUnorderedTransparent = areUnorderedTransparent[surfaceIdx];

			var opaqueLayer = isOneSided ? RenderingLayer.OneSidedOpaque : RenderingLayer.TwoSidedOpaque;
			var transparentLayer = isUnorderedTransparent ?
				RenderingLayer.UnorderedTransparent :
				(isOneSided ? RenderingLayer.OneSidedBackToFrontTransparent : RenderingLayer.TwoSidedBackToFrontTransparent);

			if (pass.Layer == opaqueLayer) {
				material.Apply(context, pass.OutputMode);
				surface.DrawOpaque(context);
				material.Unapply(context);
			}

			if (pass.Layer == transparentLayer) {
				material.Apply(context, pass.OutputMode);
				surface.DrawTransparent(context);
				material.Unapply(context);
			}
		}
	}
}
