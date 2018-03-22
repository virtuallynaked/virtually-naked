using SharpDX.Direct3D11;
using System;

public class OccluderParametersResources : IDisposable {
	public static OccluderParametersResources Make(Device device, OccluderParameters parameters) {
		return parameters != null ? new OccluderParametersResources(device, parameters) : null;
	}
	
	public readonly int vertexCount;
	
	public readonly StructuredBufferManager<float> channelWeightsBufferManager;
	public readonly ShaderResourceView baseOcclusionView;
	public readonly ShaderResourceView occlusionDeltaWeightSegmentsView;
	public readonly ShaderResourceView occlusionDeltaWeightElemsView;
	public readonly InOutStructuredBufferManager<uint> calculatedInfosBuffersBufferManager;

	private OccluderParametersResources(Device device, OccluderParameters parameters) {
		vertexCount = parameters.BaseOcclusion.Length;
					
		channelWeightsBufferManager = new StructuredBufferManager<float>(device, parameters.ChannelNames.Count);
		baseOcclusionView = BufferUtilities.ToStructuredBufferView(device, parameters.BaseOcclusion);
		occlusionDeltaWeightSegmentsView = BufferUtilities.ToStructuredBufferView(device, parameters.Deltas.Segments);
		occlusionDeltaWeightElemsView = BufferUtilities.ToStructuredBufferView(device, parameters.Deltas.Elems);
		calculatedInfosBuffersBufferManager = new InOutStructuredBufferManager<uint>(device, parameters.BaseOcclusion.Length);
	}

	public void Dispose() {
		channelWeightsBufferManager.Dispose();
		baseOcclusionView.Dispose();
		occlusionDeltaWeightSegmentsView.Dispose();
		occlusionDeltaWeightElemsView.Dispose();
		calculatedInfosBuffersBufferManager.Dispose();
	}
}
