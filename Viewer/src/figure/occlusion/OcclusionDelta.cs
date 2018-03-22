public struct OcclusionDelta {
	public int ChannelIdx { get; }
	public uint PackedOcclusionInfo { get; }

	public OcclusionDelta(int channelIdx, uint packedOcclusionInfo) {
		ChannelIdx = channelIdx;
		PackedOcclusionInfo = packedOcclusionInfo;
	}
}
