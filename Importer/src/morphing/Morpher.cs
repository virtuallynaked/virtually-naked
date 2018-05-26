using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class Morpher {
	private readonly List<Morph> morphs;

	public Morpher(List<Morph> morphs) {
		this.morphs = morphs;
	}

	public List<Morph> Morphs => morphs;

	public void Apply(ChannelOutputs channelOutputs, Vector3[] vertices) {
		for (int morphIdx = 0; morphIdx < morphs.Count; ++morphIdx) {
			morphs[morphIdx].Apply(channelOutputs, vertices);
		}
	}

	public PackedLists<VertexDelta> ConvertToVertexDeltas(int vertexCount, bool[] channelsToInclude) {
		List<List<VertexDelta>> vertexDeltas = Enumerable.Range(0, vertexCount)
			.Select(idx => new List<VertexDelta>())
			.ToList();
		
		int morphCount = Morphs.Count;
		for (int morphIdx = 0; morphIdx < morphCount; ++morphIdx) {
			Morph morph = morphs[morphIdx];

			if (channelsToInclude != null && !channelsToInclude[morph.Channel.Index]) {
				continue;
			}
			
			foreach (var delta in morph.Deltas) {
				vertexDeltas[delta.VertexIdx].Add(new VertexDelta(morphIdx, delta.PositionOffset));
			}
		}

		return PackedLists<VertexDelta>.Pack(vertexDeltas);
	}

	public List<WeightedHdMorph> LoadActiveHdMorphs(ChannelOutputs channelOutputs) {
		List<WeightedHdMorph> hdMorphs = new List<WeightedHdMorph>();

		foreach (var morph in morphs) {
			float weight = (float) morph.Channel.GetValue(channelOutputs);
			if (weight == 0) {
				continue;
			}

			var hdFile = morph.HdFile;
			if (hdFile == null) {
				continue;
			}

			var hdMorph = HdMorphSerialization.LoadHdMorph(hdFile);

			hdMorphs.Add(new WeightedHdMorph(hdMorph, weight));
		}

		return hdMorphs;
	}
}
