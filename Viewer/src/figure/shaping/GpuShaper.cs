using SharpDX.Direct3D11;
using System;
using System.Linq;

public class GpuShaper : IDisposable {
	private const int ShaderNumThreads = 64;

	private readonly Device device;
	private readonly ComputeShader withDeltasShader;
	private readonly ComputeShader withoutDeltasShader;
		
	private readonly int vertexCount;
	private readonly int[] morphChannelIndices;
	private readonly int[] boneIndices;

	private readonly ShaderResourceView initialPositionsView;
	private readonly ShaderResourceView deltaSegmentsView;
	private readonly ShaderResourceView deltaElemsView;
	private readonly StructuredBufferManager<float> morphWeightsBufferManager;
	private readonly ShaderResourceView baseDeltaWeightSegmentsView;
	private readonly ShaderResourceView baseDeltaWeightElemsView;
	private readonly ShaderResourceView boneWeightSegmentsView;
	private readonly ShaderResourceView boneWeightElemsView;
	private readonly StructuredBufferManager<StagedSkinningTransform> boneTransformsBufferManager;

	public GpuShaper(Device device, ShaderCache shaderCache, ShaperParameters parameters) {
		this.device = device;
		this.withDeltasShader = shaderCache.GetComputeShader<GpuShaper>("figure/shaping/shader/Shaper-WithDeltas");
		this.withoutDeltasShader = shaderCache.GetComputeShader<GpuShaper>("figure/shaping/shader/Shaper-WithoutDeltas");
		
		this.vertexCount = parameters.InitialPositions.Length;
		this.morphChannelIndices = parameters.MorphChannelIndices;
		this.boneIndices = parameters.BoneIndices;

		this.initialPositionsView = BufferUtilities.ToStructuredBufferView(device, parameters.InitialPositions);
		this.deltaSegmentsView = BufferUtilities.ToStructuredBufferView(device, parameters.MorphDeltas.Segments);
		this.deltaElemsView = BufferUtilities.ToStructuredBufferView(device, parameters.MorphDeltas.Elems);
		this.morphWeightsBufferManager = new StructuredBufferManager<float>(device, parameters.MorphCount);
		
		if (parameters.BaseDeltaWeights != null) {
			this.baseDeltaWeightSegmentsView = BufferUtilities.ToStructuredBufferView(device, parameters.BaseDeltaWeights.Segments);
			this.baseDeltaWeightElemsView = BufferUtilities.ToStructuredBufferView(device, parameters.BaseDeltaWeights.Elems);
		} else {
			this.baseDeltaWeightSegmentsView = null;
			this.baseDeltaWeightElemsView = null;
		}
		
		this.boneWeightSegmentsView = BufferUtilities.ToStructuredBufferView(device, parameters.BoneWeights.Segments);
		this.boneWeightElemsView = BufferUtilities.ToStructuredBufferView(device, parameters.BoneWeights.Elems);
		this.boneTransformsBufferManager = new StructuredBufferManager<StagedSkinningTransform>(device, parameters.BoneCount);
	}

	public void Dispose() {
		initialPositionsView.Dispose();
		deltaSegmentsView.Dispose();
		deltaElemsView.Dispose();
		morphWeightsBufferManager.Dispose();
		baseDeltaWeightSegmentsView?.Dispose();
		baseDeltaWeightElemsView?.Dispose();
		boneWeightSegmentsView.Dispose();
		boneWeightElemsView.Dispose();
		boneTransformsBufferManager.Dispose();
	}

	public void SetValues(DeviceContext context, ChannelOutputs channelOutputs, StagedSkinningTransform[] allBoneTransforms) {
		float[] morphWeights = morphChannelIndices.Select(idx => (float) channelOutputs.Values[idx]).ToArray();
		StagedSkinningTransform[] boneTransforms = boneIndices.Select(idx => allBoneTransforms[idx]).ToArray();

		context.WithEvent("GpuShader::SetValues", () => {
			morphWeightsBufferManager.Update(context, morphWeights);
			boneTransformsBufferManager.Update(context, boneTransforms);
		});
	}
	
	private void CalculatePositionsCommon(
		DeviceContext context,
		UnorderedAccessView vertexInfosOutView,
		ShaderResourceView occlusionInfosView,
		ShaderResourceView parentDeltasInView,
		UnorderedAccessView deltasOutView) {

		context.WithEvent("GpuShaper::CalculatePositions", () => {
			context.ClearState();

			if (deltasOutView != null) {
				context.ComputeShader.Set(withDeltasShader);
			} else {
				context.ComputeShader.Set(withoutDeltasShader);
			}
			
			context.ComputeShader.SetShaderResources(0,
				initialPositionsView,
				deltaSegmentsView,
				deltaElemsView,
				morphWeightsBufferManager.View,
				baseDeltaWeightSegmentsView,
				baseDeltaWeightElemsView,
				boneWeightSegmentsView,
				boneWeightElemsView,
				boneTransformsBufferManager.View,
				occlusionInfosView,
				parentDeltasInView);

			context.ComputeShader.SetUnorderedAccessViews(0,
				vertexInfosOutView,
				deltasOutView);

			context.Dispatch(IntegerUtils.RoundUp(vertexCount, ShaderNumThreads), 1, 1);

			context.ClearState();
		});
	}
	
	public void CalculatePositionsAndDeltas(
		DeviceContext context,
		UnorderedAccessView vertexInfosOutView,
		ShaderResourceView occlusionInfosView,
		UnorderedAccessView deltasOutView) {
		CalculatePositionsCommon(context, vertexInfosOutView, occlusionInfosView, null, deltasOutView);
	}

	public void CalculatePositions(
		DeviceContext context,
		UnorderedAccessView vertexInfosOutView,
		ShaderResourceView occlusionInfosView,
		ShaderResourceView parentDeltasView) {
		CalculatePositionsCommon(context, vertexInfosOutView, occlusionInfosView, parentDeltasView, null);
	}
}
