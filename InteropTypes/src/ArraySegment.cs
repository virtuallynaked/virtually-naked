using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct ArraySegment {
	public int Offset {get;}
	public int Count {get;}

	public ArraySegment(int offset, int count) {
		Offset = offset;
		Count = count;
	}
}
