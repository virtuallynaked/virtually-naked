using System.Collections.Generic;
using System.Linq;

public class WeightedIndexMerger {
	private Dictionary<int, float> weights = new Dictionary<int, float>();

	public void Merge(WeightedIndex weightedIndex, float weightCoefficient) {
		weights.TryGetValue(weightedIndex.Index, out float previousWeight);
		weights[weightedIndex.Index] = previousWeight + weightCoefficient * weightedIndex.Weight;
	}

	public void Merge(IEnumerable<WeightedIndex> weightedIndices, float weightCoefficient) {
		foreach (var weightedIndex in weightedIndices) {
			Merge(weightedIndex, weightCoefficient);
		}
	}

	public List<WeightedIndex> GetResult() {
		return weights.Select(pair => new WeightedIndex(pair.Key, pair.Value)).ToList();
	}
}
