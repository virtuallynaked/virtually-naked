using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct WeightedIndex {
	public int Index { get; }
	public float Weight { get; }

	public WeightedIndex(int index, float weight) {
		Index = index;
		Weight = weight;
	}
}
