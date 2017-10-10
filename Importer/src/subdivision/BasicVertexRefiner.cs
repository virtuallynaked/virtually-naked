using SharpDX.Direct3D11;
using System;
using Device = SharpDX.Direct3D11.Device;

public class BasicVertexRefiner : IDisposable {
	private static readonly int ShaderNumThreads = 64;

	private readonly ComputeShader refinerShader;

	private readonly int refinedVertexCount;
	
	private readonly ShaderResourceView stencilSegmentsView;
	private readonly ShaderResourceView stencilElemsView;
	
	public BasicVertexRefiner(Device device, ShaderCache shaderCache, PackedLists<WeightedIndexWithDerivatives> stencils) {
		this.refinedVertexCount = stencils.Count;

		this.refinerShader = shaderCache.GetComputeShader<BasicVertexRefiner>("subdivision/BasicVertexRefiner");

		this.stencilSegmentsView = BufferUtilities.ToStructuredBufferView(device, stencils.Segments);
		this.stencilElemsView = BufferUtilities.ToStructuredBufferView(device, stencils.Elems);
	}
	
	public void Dispose() {
		stencilSegmentsView.Dispose();
		stencilElemsView.Dispose();
	}

	public int RefinedVertexCount => refinedVertexCount;
	
	public void Refine(DeviceContext context, ShaderResourceView vertexPositionsView, UnorderedAccessView resultsView) {
		context.ClearState();
		context.ComputeShader.Set(refinerShader);
		context.ComputeShader.SetShaderResources(0, stencilSegmentsView, stencilElemsView, vertexPositionsView);
		context.ComputeShader.SetUnorderedAccessView(0, resultsView);
		context.Dispatch(IntegerUtils.RoundUp(refinedVertexCount, ShaderNumThreads), 1, 1);
		context.ClearState();
	}
}
