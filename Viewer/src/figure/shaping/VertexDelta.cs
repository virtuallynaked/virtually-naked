using SharpDX;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct VertexDelta {
	public int MorphIdx { get; }
	public Vector3 PositionOffset { get; }

	public VertexDelta(int morphIdx, Vector3 positionOffset) {
		MorphIdx = morphIdx;
		PositionOffset = positionOffset;
	}
}
