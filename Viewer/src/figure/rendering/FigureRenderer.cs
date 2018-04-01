using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using Device = SharpDX.Direct3D11.Device;
using System;

public class FigureRenderer : IDisposable {
	private readonly Scatterer scatterer;
	private readonly VertexRefiner vertexRefiner;
	private readonly FigureSurface[] surfaces;
	private readonly MaterialSet materialSet;
	private readonly VertexShader vertexShader;
	private readonly VertexShader falseDepthVertexShader;
	private readonly InputLayout inputLayout;
	private readonly int[][] surfaceRenderOrderByLayer;

	public FigureRenderer(Device device, ShaderCache shaderCache, Scatterer scatterer, VertexRefiner vertexRefiner, FigureSurface[] surfaces, MaterialSet materialSet, int[][] surfaceRenderOrderByLayer) {
		this.scatterer = scatterer;
		this.vertexRefiner = vertexRefiner;
		this.surfaces = surfaces;
		this.materialSet = materialSet;
		this.surfaceRenderOrderByLayer = surfaceRenderOrderByLayer;
		
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
		var orderedSurfaceIdxs = surfaceRenderOrderByLayer[(int) pass.Layer];
		if (orderedSurfaceIdxs == null || orderedSurfaceIdxs.Length == 0) {
			return;
		}

		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        context.InputAssembler.SetVertexBuffers(0, vertexRefiner.RefinedVertexBufferBinding);
		context.InputAssembler.InputLayout = inputLayout;

		var vertexShaderForMode = pass.OutputMode == OutputMode.FalseDepth ? falseDepthVertexShader : vertexShader;
		context.VertexShader.Set(vertexShaderForMode);

		foreach (int surfaceIdx in orderedSurfaceIdxs) {
			var surface = surfaces[surfaceIdx];
			var material = materialSet.Materials[surfaceIdx];

			material.Apply(context, pass);
			surface.Draw(context);
			material.Unapply(context);
		}
	}
}
