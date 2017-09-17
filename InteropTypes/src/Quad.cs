using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Quad {
	public const int SideCount = 4;

	public int Index0 { get; }
	public int Index1 { get; }
	public int Index2 { get; }
	public int Index3 { get; }

	public Quad(int index0, int index1, int index2, int index3) {
		this.Index0 = index0;
		this.Index1 = index1;
		this.Index2 = index2;
		this.Index3 = index3;
	}
	
	public int GetCorner(int idx) {
		idx %= 4;
		if (idx < 0) {
			idx += 4;
		}
		switch (idx % 4) {
			case 0: return Index0;
			case 1: return Index1;
			case 2: return Index2;
			default: return Index3;
		}
	}
	
	public Quad Flip() {
		return new Quad(Index3, Index2, Index1, Index0);
	}

	public bool Contains(int vertexIdx) {
		return Index0 == vertexIdx || Index1 == vertexIdx || Index2 == vertexIdx || Index3 == vertexIdx;
	}

	public Quad Map(Func<int, int> func) {
		return new Quad(
			func(Index0),
			func(Index1),
			func(Index2),
			func(Index3)
		);
	}
	
	public override string ToString() {
		return string.Format("Quad[{0}, {1}, {2}, {3}]", Index0, Index1, Index2, Index3);
	}

	public Quad Reindex(int offset) {
		return new Quad(Index0 + offset, Index1 + offset, Index2 + offset, Index3 + offset); 
	}
}
