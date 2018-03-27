using System.Collections.Generic;
using SharpDX.Direct3D11;
using System;

public class StaticOccluder : IOccluder {
	private readonly Device device;
	
	private readonly OcclusionInfo[] parentOcclusionInfos;

	private readonly StructuredBufferManager<uint> occlusionInfosBufferManager;
	private uint[] occlusionInfosToUpload;
	
	public StaticOccluder(Device device, OcclusionInfo[] figureOcclusionInfos, OcclusionInfo[] parentOcclusionInfos) {
		this.device = device;
		
		this.parentOcclusionInfos = parentOcclusionInfos;

		occlusionInfosBufferManager = new StructuredBufferManager<uint>(device, figureOcclusionInfos.Length);
		occlusionInfosToUpload = OcclusionInfo.PackArray(figureOcclusionInfos);
	}

	public void Dispose() {
		occlusionInfosBufferManager.Dispose();
	}

	public OcclusionInfo[] ParentOcclusionInfos => parentOcclusionInfos;

	public ShaderResourceView OcclusionInfosView => occlusionInfosBufferManager.View;
	
	public void RegisterChildOccluders(List<IOccluder> childOccluders) {
		if (childOccluders.Count > 0) {
			throw new InvalidOperationException("static occluders cannot have children");
		}
	}

	public void SetValues(DeviceContext context, ChannelOutputs channelOutputs) {
		//do nothing
	}
	
	public void CalculateOcclusion(DeviceContext context) {
		if (occlusionInfosToUpload != null) {
			occlusionInfosBufferManager.Update(context, occlusionInfosToUpload);
			occlusionInfosToUpload = null;
		}
	}
}
