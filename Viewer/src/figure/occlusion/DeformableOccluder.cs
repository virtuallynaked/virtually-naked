using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class DeformableOccluder : IOccluder {
	private const int ShaderNumThreads = 64;
	
	private readonly Device device;
		
	private ComputeShader shader;
	
	private readonly OcclusionInfo[] unmorphedOcclusionInfos;

	public readonly ShaderResourceView unmorphedWithoutChildrenOcclusionInfosView;
	private readonly StructuredBufferManager<uint> unmorphedWithChildrenOcclusionInfosBufferManager;

	private readonly OccluderParametersResources parametersResources;
		
	public DeformableOccluder(Device device, ShaderCache shaderCache, OcclusionInfo[] unmorphedOcclusionInfos, OccluderParameters parameters) {
		this.device = device;
		this.unmorphedOcclusionInfos = unmorphedOcclusionInfos;
				
		shader = shaderCache.GetComputeShader<DeformableOccluder>("figure/occlusion/shader/Occluder");
		
		unmorphedWithoutChildrenOcclusionInfosView = BufferUtilities.ToStructuredBufferView(device, OcclusionInfo.PackArray(unmorphedOcclusionInfos));
		unmorphedWithChildrenOcclusionInfosBufferManager = new StructuredBufferManager<uint>(device, unmorphedOcclusionInfos.Length);
		RegisterChildOccluders(new List<IOccluder>() { });
		
		parametersResources = OccluderParametersResources.Make(device, parameters);
	}
	
	public void Dispose() {
		unmorphedWithoutChildrenOcclusionInfosView.Dispose();
		unmorphedWithChildrenOcclusionInfosBufferManager.Dispose();
		parametersResources?.Dispose();
	}
	
	public ShaderResourceView OcclusionInfosView => parametersResources?.calculatedInfosBuffersBufferManager?.InView ?? unmorphedWithChildrenOcclusionInfosBufferManager.View;

	public OcclusionInfo[] ParentOcclusionInfos => throw new InvalidOperationException("deformable occluders cannot be children");

	public void RegisterChildOccluders(List<IOccluder> childOccluders) {
		List<OcclusionInfo[]> childOcclusionContributions = childOccluders
			.Select(occluder => occluder.ParentOcclusionInfos)
			.ToList();
		IncorporateChildOcclusionContributions(childOcclusionContributions);
	}

	private void IncorporateChildOcclusionContributions(List<OcclusionInfo[]> childOcclusionContributions) {
		int count = unmorphedOcclusionInfos.Length;
		OcclusionInfoBlender[] blenders = new OcclusionInfoBlender[count];
		for (int i = 0; i < count; ++i) {
			blenders[i].Init(unmorphedOcclusionInfos[i]);
		}
		
		foreach (OcclusionInfo[] childOcclusionContribution in childOcclusionContributions) {
			if (childOcclusionContribution.Length != count) {
				throw new InvalidOperationException("parent-child occlusion info length mismatch");
			}

			for (int i = 0; i < count; ++i) {
				blenders[i].Add(childOcclusionContribution[i], unmorphedOcclusionInfos[i]);
			}
		}
		
		OcclusionInfo[] results = new OcclusionInfo[count];
		for (int i = 0; i < count; ++i) {
			results[i] = blenders[i].GetResult();
		}
		
		unmorphedWithChildrenOcclusionInfosBufferManager.Update(OcclusionInfo.PackArray(results));
	}

	public void SetValues(ChannelOutputs channelOutputs) {
		if (parametersResources == null) {
			return;
		}

		float[] weights = parametersResources.Parameters.ChannelIndices.Select(idx => (float) channelOutputs.Values[idx]).ToArray();
		parametersResources.channelWeightsBufferManager.Update(weights);
	}

	public void CalculateOcclusion() {
		if (parametersResources == null) {
			return;
		}

		DeviceContext context = device.ImmediateContext;

		context.WithEvent("Occluder::CalculateOcclusion", () => {
			context.ClearState();

			context.ComputeShader.Set(shader);
			context.ComputeShader.SetShaderResources(0,
				unmorphedWithoutChildrenOcclusionInfosView,
				unmorphedWithChildrenOcclusionInfosBufferManager.View,
				parametersResources.baseOcclusionView,
				parametersResources.channelWeightsBufferManager.View,
				parametersResources.occlusionDeltaWeightSegmentsView,
				parametersResources.occlusionDeltaWeightElemsView);
			context.ComputeShader.SetUnorderedAccessView(0, parametersResources.calculatedInfosBuffersBufferManager.OutView);

			context.Dispatch(IntegerUtils.RoundUp(parametersResources.vertexCount, ShaderNumThreads), 1, 1);
			
			context.ClearState();
		});
	}
}
