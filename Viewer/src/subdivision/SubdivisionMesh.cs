using System;

public class SubdivisionMesh {
	public static readonly SubdivisionMesh Empty = new SubdivisionMesh(
		0,
		QuadTopology.Empty,
		PackedLists<WeightedIndexWithDerivatives>.Empty);

	public int ControlVertexCount { get; }
	public QuadTopology Topology { get; }
	public PackedLists<WeightedIndexWithDerivatives> Stencils { get; }

	public SubdivisionMesh(int controlVertexCount, QuadTopology topology, PackedLists<WeightedIndexWithDerivatives> stencils) {
		if (stencils.Count != topology.VertexCount) {
			throw new ArgumentException("vertex count mismatch");
		}

		ControlVertexCount = controlVertexCount;
		Topology = topology;
		Stencils = stencils;
	}

	public static SubdivisionMesh Combine(SubdivisionMesh meshA, SubdivisionMesh meshB) {
		int offset = meshA.ControlVertexCount;

		return new SubdivisionMesh(
			meshA.ControlVertexCount + meshB.ControlVertexCount,
			QuadTopology.Combine(meshA.Topology, meshB.Topology),
			PackedLists<WeightedIndexWithDerivatives>.Concat(
				meshA.Stencils,
				meshB.Stencils.Map(w => w.Reindex(offset))));
	}
}
