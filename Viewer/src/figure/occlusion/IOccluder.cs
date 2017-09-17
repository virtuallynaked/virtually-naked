using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;

public interface IOccluder : IDisposable {
	OcclusionInfo[] ParentOcclusionInfos { get; }
	void RegisterChildOccluders(List<IOccluder> childOccluders);

	void SetValues(ChannelOutputs channelOutputs);
	void CalculateOcclusion();


	ShaderResourceView OcclusionInfosView { get; }
}
