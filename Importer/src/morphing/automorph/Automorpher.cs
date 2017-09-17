using SharpDX;

public class Automorpher {
	private readonly PackedLists<WeightedIndex> baseDeltaWeights;

	public Automorpher(PackedLists<WeightedIndex> baseDeltaWeights) {
		this.baseDeltaWeights = baseDeltaWeights;
	}

	public PackedLists<WeightedIndex> BaseDeltaWeights => baseDeltaWeights;
	
	public void Apply(Vector3[] baseDeltas, Vector3[] controlPositions) {
		for (int childVertexIdx = 0; childVertexIdx < controlPositions.Length; ++childVertexIdx) {
			Vector3 delta = Vector3.Zero;
			foreach (var baseDeltaWeight in baseDeltaWeights.GetElements(childVertexIdx)) {
				delta += baseDeltaWeight.Weight * baseDeltas[baseDeltaWeight.Index];
			}

			controlPositions[childVertexIdx] += delta;
		}
	}
}
