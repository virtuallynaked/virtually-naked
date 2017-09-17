using SharpDX;

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

	public ShaperParameters(
		Vector3[] initialPositions,
		int morphCount, int[] morphChannelIndices, PackedLists<VertexDelta> morphDeltas,
		PackedLists<WeightedIndex> baseDeltaWeights,
		int boneCount, int[] boneIndices, PackedLists<BoneWeight> boneWeights) {
		InitialPositions = initialPositions;

		MorphCount = morphCount;
		MorphChannelIndices = morphChannelIndices;
		MorphDeltas = morphDeltas;

		BaseDeltaWeights = baseDeltaWeights;

		BoneCount = boneCount;
		BoneIndices = boneIndices;
		BoneWeights = boneWeights;
	}
}
