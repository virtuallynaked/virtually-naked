using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Tri {
	public const int SideCount = 3;

	public int Index0 { get; }
	public int Index1 { get; }
	public int Index2 { get; }

	public Tri(int index0, int index1, int index2) {
		this.Index0 = index0;
		this.Index1 = index1;
		this.Index2 = index2;
	}
	
	public int GetCorner(int idx) {
		idx %= SideCount;
		if (idx < 0) {
			idx += SideCount;
		}
		switch (idx % SideCount) {
			case 0: return Index0;
			case 1: return Index1;
			default: return Index2;
		}
	}
	
	public Tri Flip() {
		return new Tri(Index2, Index1, Index0);
	}

	public bool Contains(int vertexIdx) {
		return Index0 == vertexIdx || Index1 == vertexIdx || Index2 == vertexIdx;
	}

	public Tri Map(Func<int, int> func) {
		return new Tri(
			func(Index0),
			func(Index1),
			func(Index2)
		);
	}
	
	public override string ToString() {
		return string.Format("Quad[{0}, {1}, {2}, {3}]", Index0, Index1, Index2);
	}

	public Tri Reindex(int offset) {
		return new Tri(Index0 + offset, Index1 + offset, Index2 + offset); 
	}
}
