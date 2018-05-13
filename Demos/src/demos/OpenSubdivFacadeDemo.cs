using OpenSubdivFacade;
using System;

public class OpenSubdivFacadeDemo :IDemoApp {
	public void Run() {
		int controlVertexCount = 6;
		Quad[] controlFaces = new [] {
			new Quad(0, 1, 2, 3),
			new Quad(1, 4, 5, 2) };
		QuadTopology controlTopology = new QuadTopology(controlVertexCount, controlFaces);
		
		int refinementLevel = 1;

		using (Refinement refinement = new Refinement(controlTopology, refinementLevel)) {
			QuadTopology topology = refinement.GetTopology();
			int[] faceMap = refinement.GetFaceMap();
						
			for (int faceIdx = 0; faceIdx < topology.Faces.Length; ++faceIdx) {
				Console.WriteLine(topology.Faces[faceIdx] + " -> " + faceMap[faceIdx]);
			}
			Console.WriteLine();
			
			PackedLists<WeightedIndex> stencils = refinement.GetStencils(StencilKind.LimitStencils);
			Console.WriteLine("stencils: ");
			for (int vertexIdx = 0; vertexIdx < stencils.Count; ++vertexIdx) {
				Console.WriteLine(vertexIdx + ":");
				foreach (WeightedIndex weightedIndex in stencils.GetElements(vertexIdx)) {
					Console.WriteLine("\t" + weightedIndex.Index + " -> " + weightedIndex.Weight);
				}
			}
		}
	}
}
