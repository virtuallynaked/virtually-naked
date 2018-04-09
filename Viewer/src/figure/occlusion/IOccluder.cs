using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;

public interface IOccluder : IDisposable {
	OcclusionInfo[] ParentOcclusionInfos { get; }
	void SetChildOccluders(List<IOccluder> childOccluders);

	void SetValues(DeviceContext context, ChannelOutputs channelOutputs);
	void CalculateOcclusion(DeviceContext context);


	ShaderResourceView OcclusionInfosView { get; }
}
