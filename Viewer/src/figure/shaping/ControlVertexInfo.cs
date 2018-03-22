using SharpDX;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct ControlVertexInfo {
	public Vector3 position;
	public int packedOcclusionInfo;
}
