using System.Collections.Generic;

public class OccluderParameters {
	public uint[] BaseOcclusion { get; }
	public List<string> ChannelNames { get; }
	public PackedLists<OcclusionDelta> Deltas { get; }

	public OccluderParameters(uint[] baseOcclusion, List<string> channelNames, PackedLists<OcclusionDelta> deltas) {
		BaseOcclusion = baseOcclusion;
		ChannelNames = channelNames;
		Deltas = deltas;
	}
}
