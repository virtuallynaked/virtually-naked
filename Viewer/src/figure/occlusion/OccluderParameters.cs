public class OccluderParameters {
	public uint[] BaseOcclusion { get; }
	public int[] ChannelIndices { get; }
	public PackedLists<OcclusionDelta> Deltas { get; }

	public OccluderParameters(uint[] baseOcclusion, int[] channelIndices, PackedLists<OcclusionDelta> deltas) {
		BaseOcclusion = baseOcclusion;
		ChannelIndices = channelIndices;
		Deltas = deltas;
	}
}
