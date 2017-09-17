using System.Collections.Generic;
using SharpDX.Direct3D11;
using System;

public class StaticOccluder : IOccluder {
	private readonly Device device;
	
	private readonly OcclusionInfo[] parentOcclusionInfos;

	private readonly StructuredBufferManager<uint> occlusionInfosBufferManager;
	
	public StaticOccluder(Device device, OcclusionInfo[] figureOcclusionInfos, OcclusionInfo[] parentOcclusionInfos) {
		this.device = device;
		
		this.parentOcclusionInfos = parentOcclusionInfos;

		occlusionInfosBufferManager = new StructuredBufferManager<uint>(device, figureOcclusionInfos.Length);
		occlusionInfosBufferManager.Update(OcclusionInfo.PackArray(figureOcclusionInfos));
	}

	public void Dispose() {
		occlusionInfosBufferManager.Dispose();
	}

	public OcclusionInfo[] ParentOcclusionInfos => parentOcclusionInfos;

	public ShaderResourceView OcclusionInfosView => occlusionInfosBufferManager.View;
	
	public void RegisterChildOccluders(List<IOccluder> childOccluders) {
		throw new InvalidOperationException("static occluders cannot have children");
	}

	public void SetValues(ChannelOutputs channelOutputs) {
		//do nothing
	}
	
	public void CalculateOcclusion() {
		//do nothing
	}
}
