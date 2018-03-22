using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SubdivisionMeshPersistance {
	public const string StencilSegmentsFilename = "stencil-segments.array";
	public const string StencilElementsFilename = "stencil-elements.array";
	public const string StencilFacesFilename = "faces.array";

	public static void Save(System.IO.DirectoryInfo directory, SubdivisionMesh mesh) {
		directory.File(StencilSegmentsFilename).WriteArray(mesh.Stencils.Segments);
		directory.File(StencilElementsFilename).WriteArray(mesh.Stencils.Elems);
		directory.File(StencilFacesFilename).WriteArray(mesh.Topology.Faces);
	}

	public static SubdivisionMesh Load(IArchiveDirectory directory) {
		var stencilSegments = directory.File(StencilSegmentsFilename).ReadArray<ArraySegment>();
		var stencilElements = directory.File(StencilElementsFilename).ReadArray<WeightedIndexWithDerivatives>();
		var faces = directory.File(StencilFacesFilename).ReadArray<Quad>();

		var stencils = new PackedLists<WeightedIndexWithDerivatives>(stencilSegments, stencilElements);

		int vertexCount = stencils.Count;
		var topology = new QuadTopology(vertexCount, faces);

		return new SubdivisionMesh(0, topology, stencils);
	}
}
