using SharpDX;

public struct MorphDelta {
	public int VertexIdx { get; }
	public Vector3 PositionOffset { get; }

	public MorphDelta(int vertexIdx, Vector3 positionOffset) {
		VertexIdx = vertexIdx;
		PositionOffset = positionOffset;
	}
}
