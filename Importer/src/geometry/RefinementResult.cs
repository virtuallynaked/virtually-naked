using OpenSubdivFacade;
using System;
using System.Collections.Generic;
using System.Linq;

public class RefinementResult {
	public static readonly RefinementResult Empty = new RefinementResult(
		SubdivisionMesh.Empty,
		SubdivisionTopologyInfo.Empty,
		new int[0]);

	public SubdivisionMesh Mesh { get; }
	public SubdivisionTopologyInfo TopologyInfo { get; }
	public int[] ControlFaceMap { get; }

	public RefinementResult(SubdivisionMesh mesh, SubdivisionTopologyInfo topologyInfo, int[] controlFaceMap) {
		Mesh = mesh;
		TopologyInfo = topologyInfo;
		ControlFaceMap = controlFaceMap;
	}

	public static RefinementResult Make(QuadTopology controlTopology, int[] controlSurfaceMap, int refinementLevel, bool derivativesOnly) {
		if (controlTopology.Faces.Length == 0 && controlTopology.VertexCount == 0) {
			return RefinementResult.Empty;
		}

		PackedLists<WeightedIndex> limitStencils, limitDuStencils, limitDvStencils;
		QuadTopology refinedTopology;
		SubdivisionTopologyInfo refinedTopologyInfo;
		int[] controlFaceMap;
		using (var refinement = new Refinement(controlTopology, refinementLevel)) {
			limitStencils = refinement.GetStencils(StencilKind.LimitStencils);
			limitDuStencils = refinement.GetStencils(StencilKind.LimitDuStencils);
			limitDvStencils = refinement.GetStencils(StencilKind.LimitDvStencils);
			refinedTopology = refinement.GetTopology();

			var adjacentVertices = refinement.GetAdjacentVertices();
			var rules = refinement.GetVertexRules();
			refinedTopologyInfo = new SubdivisionTopologyInfo(adjacentVertices, rules);

			controlFaceMap = refinement.GetFaceMap();
		}

		if (derivativesOnly) {
			if (refinementLevel != 0) {
				throw new InvalidOperationException("derivatives-only mode can only be used at refinement level 0");
			}

			limitStencils = PackedLists<WeightedIndex>.Pack(Enumerable.Range(0, controlTopology.VertexCount)
				.Select(vertexIdx => {
					var selfWeight = new WeightedIndex(vertexIdx, 1);
					return new List<WeightedIndex> { selfWeight };
				}).ToList());
		}

		PackedLists<WeightedIndexWithDerivatives> stencils = WeightedIndexWithDerivatives.Merge(limitStencils, limitDuStencils, limitDvStencils);
		var refinedMesh = new SubdivisionMesh(controlTopology.VertexCount, refinedTopology, stencils);
		
		return new RefinementResult(refinedMesh, refinedTopologyInfo, controlFaceMap);
	}

	public static RefinementResult Combine(RefinementResult resultA, RefinementResult resultB) {
		SubdivisionMesh combinedMesh = SubdivisionMesh.Combine(
			resultA.Mesh,
			resultB.Mesh);

		SubdivisionTopologyInfo combinedTopologyInfo = SubdivisionTopologyInfo.Combine(
			resultA.TopologyInfo,
			resultB.TopologyInfo);

		int[] combinedControlFaceMap = Enumerable.Concat(
			resultA.ControlFaceMap,
			resultB.ControlFaceMap).ToArray();

		return new RefinementResult(combinedMesh, combinedTopologyInfo, combinedControlFaceMap);
	}
}
