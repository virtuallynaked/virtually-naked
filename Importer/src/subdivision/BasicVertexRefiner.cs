using SharpDX.Direct3D11;
using System;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX;

public class BasicVertexRefiner : IDisposable {
	private static readonly int BasicRefinedVertexInfo_SizeInBytes = Vector3.SizeInBytes + Vector3.SizeInBytes;
	private static readonly int ShaderNumThreads = 64;

	private readonly ComputeShader refinerShader;

	private readonly int refinedVertexCount;
	
	private readonly ShaderResourceView stencilSegmentsView;
	private readonly ShaderResourceView stencilElemsView;

	private readonly UnorderedAccessView outputWriteView;
	private readonly ShaderResourceView outputReadView;
	
	public BasicVertexRefiner(Device device, ShaderCache shaderCache, PackedLists<WeightedIndexWithDerivatives> stencils) {
		this.refinedVertexCount = stencils.Count;

		this.refinerShader = shaderCache.GetComputeShader<BasicVertexRefiner>("subdivision/BasicVertexRefiner");

		this.stencilSegmentsView = BufferUtilities.ToStructuredBufferView(device, stencils.Segments);
		this.stencilElemsView = BufferUtilities.ToStructuredBufferView(device, stencils.Elems);
				
		BufferDescription outputBufferDesc = new BufferDescription {
			SizeInBytes = refinedVertexCount * BasicRefinedVertexInfo_SizeInBytes,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
			OptionFlags = ResourceOptionFlags.BufferStructured,
			StructureByteStride = BasicRefinedVertexInfo_SizeInBytes
		};

		using (Buffer buffer = new Buffer(device, outputBufferDesc)) {
			this.outputWriteView = new UnorderedAccessView(device, buffer);
			this.outputReadView = new ShaderResourceView(device, buffer);
		}
	}
	
	public void Dispose() {
		refinerShader.Dispose();

		stencilSegmentsView.Dispose();
		stencilElemsView.Dispose();

		outputWriteView.Dispose();
		outputReadView.Dispose();
	}
	
	public ShaderResourceView Refine(DeviceContext context, ShaderResourceView vertexPositionsView) {
		context.ClearState();
		context.ComputeShader.Set(refinerShader);
		context.ComputeShader.SetShaderResources(0, stencilSegmentsView, stencilElemsView, vertexPositionsView);
		context.ComputeShader.SetUnorderedAccessView(0, outputWriteView);
		context.Dispatch(IntegerUtils.RoundUp(refinedVertexCount, ShaderNumThreads), 1, 1);
		context.ClearState();

		return outputReadView;
	}
}
