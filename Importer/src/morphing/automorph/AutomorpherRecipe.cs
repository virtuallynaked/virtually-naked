using OpenSubdivFacade;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class AutomorpherRecipe {
	public static AutomorpherRecipe Make(Geometry parentGeometry, Geometry childGeometry) {
		var parentLimit0Stencils = parentGeometry.MakeStencils(StencilKind.LimitStencils, 0);
		var subdivider = new Subdivider(parentLimit0Stencils);
		var parentLimit0VertexPositions = subdivider.Refine(parentGeometry.VertexPositions, new Vector3Operators());

		List<List<WeightedIndex>> baseDeltaWeights = 
			Enumerable.Range(0, childGeometry.VertexCount)
			.AsParallel().AsOrdered()
			.Select(childVertexIdx => {
				Vector3 graftVertex = childGeometry.VertexPositions[childVertexIdx];
				ClosestPoint.PointOnMesh closestPointOnBaseMesh = ClosestPoint.FindClosestPointOnMesh(parentGeometry.Faces, parentLimit0VertexPositions, graftVertex);
			
				var merger = new WeightedIndexMerger();
				merger.Merge(parentLimit0Stencils.GetElements(closestPointOnBaseMesh.VertexIdxA), closestPointOnBaseMesh.BarycentricWeights.X);
				merger.Merge(parentLimit0Stencils.GetElements(closestPointOnBaseMesh.VertexIdxB), closestPointOnBaseMesh.BarycentricWeights.Y);
				merger.Merge(parentLimit0Stencils.GetElements(closestPointOnBaseMesh.VertexIdxC), closestPointOnBaseMesh.BarycentricWeights.Z);
				
				return merger.GetResult();
			})
			.ToList();

		var packedBaseDeltaWeights = PackedLists<WeightedIndex>.Pack(baseDeltaWeights);
		return new AutomorpherRecipe(packedBaseDeltaWeights);
    }

	public PackedLists<WeightedIndex> BaseDeltaWeights { get; }

	public AutomorpherRecipe(PackedLists<WeightedIndex> baseDeltaWeights) {
		BaseDeltaWeights = baseDeltaWeights;
	}

	public Automorpher Bake() {
		return new Automorpher(BaseDeltaWeights);
	}

	private MorphRecipe Rewrite(MorphRecipe morphRecipe, Figure parentFigure, Morph parentMorph) {
		Vector3[] parentDeltas = new Vector3[parentFigure.Geometry.VertexCount];
		foreach (var delta in parentMorph.Deltas) {
			parentDeltas[delta.VertexIdx] += delta.PositionOffset;
		}

		int vertexCount = BaseDeltaWeights.Count;
		Vector3[] deltas = new Vector3[BaseDeltaWeights.Count];
		foreach (var delta in morphRecipe.Deltas) {
			deltas[delta.VertexIdx] += delta.PositionOffset;
		}
		for (int vertexIdx = 0; vertexIdx < vertexCount; ++vertexIdx) {
			foreach (var baseDeltaWeight in BaseDeltaWeights.GetElements(vertexIdx)) {
				deltas[vertexIdx] -= baseDeltaWeight.Weight * parentDeltas[baseDeltaWeight.Index];
			}
		}

		List<MorphDelta> rewrittenDeltas = new List<MorphDelta>(vertexCount);
		for (int vertexIdx = 0; vertexIdx < vertexCount; ++vertexIdx) {
			Vector3 positionOffset = deltas[vertexIdx];
			if (positionOffset.IsZero) {
				continue;
			}
			rewrittenDeltas.Add(new MorphDelta(vertexIdx, positionOffset));
		}
		return new MorphRecipe {
			Channel = morphRecipe.Channel,
			Deltas = rewrittenDeltas.ToArray()
		};
	}

	public List<MorphRecipe> Rewrite(List<MorphRecipe> morphsRecipes, Figure parentFigure) {
		var parentMorphsByName = parentFigure.Morpher.Morphs.ToDictionary(morph => morph.Name, morph => morph);

		return morphsRecipes
			.Select(recipe => {
				if (parentMorphsByName.TryGetValue(recipe.Channel, out var parentMorph)) {
					return Rewrite(recipe, parentFigure, parentMorph);
				} else {
					return recipe;
				}
			})
			.ToList();
	}
}
