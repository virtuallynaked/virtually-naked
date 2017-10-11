using SharpDX;

public class OcclusionSurrogateParameters {
	public int BoneIndex { get; }
	public int OffsetInOcclusionInfos { get; }

	public OcclusionSurrogateParameters(int boneIndex, int offsetInOcclusionInfos) {
		BoneIndex = boneIndex;
		OffsetInOcclusionInfos = offsetInOcclusionInfos;
	}
}

public class ShaperParameters {
	public Vector3[] InitialPositions { get; }

	//Morpher
	public int MorphCount { get; }
	public int[] MorphChannelIndices { get; }
	public PackedLists<VertexDelta> MorphDeltas { get; }

	//Automorpher
	public PackedLists<WeightedIndex> BaseDeltaWeights { get; }

	//Skinner
	public int BoneCount { get; }
	public int[] BoneIndices { get; }
	public PackedLists<BoneWeight> BoneWeights { get; }

	//Occlusion Merger
	public int[] OcclusionSurrogateMap { get; }
	public OcclusionSurrogateParameters[] OcclusionSurrogateParameters { get; }

	public ShaperParameters(
		Vector3[] initialPositions,
		int morphCount, int[] morphChannelIndices, PackedLists<VertexDelta> morphDeltas,
		PackedLists<WeightedIndex> baseDeltaWeights,
		int boneCount, int[] boneIndices, PackedLists<BoneWeight> boneWeights,
		int[] occlusionSurrogateMap, OcclusionSurrogateParameters[] occlusionSurrogateParameters) {
		InitialPositions = initialPositions;

		MorphCount = morphCount;
		MorphChannelIndices = morphChannelIndices;
		MorphDeltas = morphDeltas;

		BaseDeltaWeights = baseDeltaWeights;

		BoneCount = boneCount;
		BoneIndices = boneIndices;
		BoneWeights = boneWeights;

		OcclusionSurrogateMap = occlusionSurrogateMap;
		OcclusionSurrogateParameters = occlusionSurrogateParameters;
	}
}
