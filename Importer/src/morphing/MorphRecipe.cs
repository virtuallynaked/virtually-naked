using ProtoBuf;
using System.Collections.Generic;
using System;
using System.Linq;
using SharpDX;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MorphRecipe {
	public string Channel { get; set; }
	public MorphDelta[] Deltas { get; set; }
	public string HdUrl { get; set; }
	
	public Morph Bake(ContentFileLocator fileLocator, Dictionary<string, Channel> channels) {
		var hdFile = HdUrl != null ? fileLocator.Locate(HdUrl).File : null;
		return new Morph(channels[Channel], Deltas, hdFile);
	}

	public static MorphRecipe Merge(string channelName, FigureRecipeMerger.Reindexer reindexer, MorphRecipe parentMorph, MorphRecipe[] childMorphs, AutomorpherRecipe[] childAutomorphers) {
		List<MorphDelta> morphDeltas = new List<MorphDelta>();

		Vector3[] flatPositionOffsets;

		if (parentMorph != null) {
			morphDeltas.AddRange(parentMorph.Deltas);

			flatPositionOffsets = new Vector3[reindexer.GetParentVertexCount()];
			foreach (MorphDelta parentDelta in parentMorph.Deltas) {
				flatPositionOffsets[parentDelta.VertexIdx] = parentDelta.PositionOffset;
			}
		} else {
			flatPositionOffsets = null;
		}
		
		for (int childIdx = 0; childIdx < reindexer.ChildOffsets.Length; ++childIdx) {
			int childVertexOffset = reindexer.ChildOffsets[childIdx].Vertex;
			var childMorph = childMorphs[childIdx];

			if (childMorph != null) {
				morphDeltas.AddRange(childMorph.Deltas
					.Select(delta => new MorphDelta(delta.VertexIdx + childVertexOffset, delta.PositionOffset)));
			} else {
				if (flatPositionOffsets == null) {
					continue;
				}

				//generate the deltas using the automorpher
				var automorpher = childAutomorphers[childIdx];
				for (int childVertexIdx = 0; childVertexIdx < automorpher.BaseDeltaWeights.Count; ++childVertexIdx) {
					Vector3 childPositionOffset = Vector3.Zero;
					foreach (var baseDeltaWeight in automorpher.BaseDeltaWeights.GetElements(childVertexIdx)) {
						childPositionOffset += baseDeltaWeight.Weight * flatPositionOffsets[baseDeltaWeight.Index];
					}

					if (!childPositionOffset.IsZero) {
						morphDeltas.Add(new MorphDelta(childVertexIdx + childVertexOffset, childPositionOffset));
					}
				}
			}
		}

		return new MorphRecipe {
			Channel = channelName,
			Deltas = morphDeltas.ToArray()
		};
	}
}
