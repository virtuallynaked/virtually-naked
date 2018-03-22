using System;
using System.Collections.Generic;
using System.Linq;

public struct WeightedIndexWithDerivatives {
	public int Index { get; }
	public float Weight { get; }
	public float DuWeight { get; }
	public float DvWeight { get; }

	public WeightedIndexWithDerivatives(int index, float weight, float duWeight, float dvWeight) {
		Index = index;
		Weight = weight;
		DuWeight = duWeight;
		DvWeight = dvWeight;
	}

	public WeightedIndexWithDerivatives Reindex(int offset) {
		return new WeightedIndexWithDerivatives(Index + offset, Weight, DuWeight, DvWeight);
	}

	private struct WeightWithDerivatives {
		public readonly float value;
		public readonly float du;
		public readonly float dv;

		public WeightWithDerivatives(float value, float du, float dv) {
			this.value = value;
			this.du = du;
			this.dv = dv;
		}
	}

	public static PackedLists<WeightedIndexWithDerivatives> Merge(
		PackedLists<WeightedIndex> valueStencils,
		PackedLists<WeightedIndex> duStencils,
		PackedLists<WeightedIndex> dvStencils) {
		int segmentCount = valueStencils.Count;
		if (duStencils.Count != segmentCount) {
			throw new InvalidOperationException("expected du segment count to match");
		}
		if (dvStencils.Count != segmentCount) {
			throw new InvalidOperationException("expected dv segment count to match");
		}

		var combinedStencils = new List<List<WeightedIndexWithDerivatives>>(segmentCount);

		for (int segmentIdx = 0; segmentIdx < segmentCount; ++segmentIdx) {
			IEnumerable<WeightedIndexWithDerivatives> values = valueStencils.GetElements(segmentIdx)
				.Select(w => new WeightedIndexWithDerivatives(w.Index, w.Weight, 0, 0));

			IEnumerable<WeightedIndexWithDerivatives> dvs = duStencils.GetElements(segmentIdx)
				.Select(w => new WeightedIndexWithDerivatives(w.Index, 0, w.Weight, 0));

			IEnumerable<WeightedIndexWithDerivatives> dus = dvStencils.GetElements(segmentIdx)
				.Select(w => new WeightedIndexWithDerivatives(w.Index, 0, 0, w.Weight));
			
			List<WeightedIndexWithDerivatives> combinedWeightedIndices = values.Concat(dvs).Concat(dus).GroupBy(w => w.Index).Select(group => {
				float sumWeight = group.Sum(w => w.Weight);
				float sumDuWeight = group.Sum(w => w.DuWeight);
				float sumDvWeight = group.Sum(w => w.DvWeight);
				return new WeightedIndexWithDerivatives(group.Key, sumWeight, sumDuWeight, sumDvWeight);
			}).ToList();

			combinedStencils.Add(combinedWeightedIndices);
		}

		return PackedLists<WeightedIndexWithDerivatives>.Pack(combinedStencils);
	}
}
