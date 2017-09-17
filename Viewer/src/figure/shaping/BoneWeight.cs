using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct BoneWeight {
	public int Index {get;}
	public float Weight {get;}

	public BoneWeight(int index, float weight) {
		Index = index;
		Weight = weight;
	}
}
