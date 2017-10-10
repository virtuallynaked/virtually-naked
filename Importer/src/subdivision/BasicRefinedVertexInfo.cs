using SharpDX;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct BasicRefinedVertexInfo {
	public Vector3 position;
	public Vector3 normal;
}
