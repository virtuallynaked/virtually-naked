using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct Vector3WeightedIndex {
	public int Index { get; }
	public Vector3 Weight { get; }

	public Vector3WeightedIndex(int index, Vector3 weight) {
		Index = index;
		Weight = weight;
	}

	public static PackedLists<Vector3WeightedIndex> Merge(
		PackedLists<WeightedIndex> values0,
		PackedLists<WeightedIndex> values1,
		PackedLists<WeightedIndex> values2) {
		int segmentCount = values0.Count;
		if (values1.Count != segmentCount) {
			throw new InvalidOperationException("expected values1 count to match");
		}
		if (values2.Count != segmentCount) {
			throw new InvalidOperationException("expected values2 count to match");
		}

		var combined = new List<List<Vector3WeightedIndex>>(segmentCount);

		for (int segmentIdx = 0; segmentIdx < segmentCount; ++segmentIdx) {
			IEnumerable<Vector3WeightedIndex> v0s = values0.GetElements(segmentIdx)
				.Select(w => new Vector3WeightedIndex(w.Index, new Vector3(w.Weight, 0, 0)));

			IEnumerable<Vector3WeightedIndex> v1s = values1.GetElements(segmentIdx)
				.Select(w => new Vector3WeightedIndex(w.Index, new Vector3(0, w.Weight, 0)));

			IEnumerable<Vector3WeightedIndex> v2s = values2.GetElements(segmentIdx)
				.Select(w => new Vector3WeightedIndex(w.Index, new Vector3(0, 0, w.Weight)));
			
			List<Vector3WeightedIndex> combinedWeightedIndices = v0s.Concat(v1s).Concat(v2s).GroupBy(w => w.Index).Select(group => {
				float sum0 = group.Sum(w => w.Weight[0]);
				float sum1 = group.Sum(w => w.Weight[1]);
				float sum2 = group.Sum(w => w.Weight[2]);
				return new Vector3WeightedIndex(group.Key, new Vector3(sum0, sum1, sum2));
			}).ToList();

			combined.Add(combinedWeightedIndices);
		}

		return PackedLists<Vector3WeightedIndex>.Pack(combined);
	}
}
