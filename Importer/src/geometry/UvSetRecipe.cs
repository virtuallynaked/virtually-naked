using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class UvSetRecipe {
	public string Name { get; }
	public Vector2[] Uvs { get; }
	public Quad[] Faces { get; }
	
	public UvSetRecipe(string name, Vector2[] uvs, Quad[] faces) {
		Name = name;
		Uvs = uvs;
		Faces = faces;
	}
	
	public void Bake(Geometry geometry, Dictionary<string, UvSet> uvSets) {
		var uvSet = new UvSet(Name, Uvs, Faces);
		uvSets.Add(Name, uvSet);
	}
	
	public static UvSetRecipe Merge(FigureRecipeMerger.Reindexer reindexer, UvSetRecipe parentUvSet, UvSetRecipe[] childUvSets) {
		List<Vector2> mergedUvs = new List<Vector2>();
		List<Quad> mergedFaces = new List<Quad>();
		
		mergedUvs.AddRange(parentUvSet.Uvs);

		Quad[] parentFaces = parentUvSet.Faces;
		for (int faceIdx = 0; faceIdx < parentFaces.Length; ++faceIdx) {
			if (reindexer.IsParentFaceHidden(faceIdx)) {
				continue;
			}
			mergedFaces.Add(parentFaces[faceIdx]);
		}
		
		for (int childIdx = 0; childIdx < childUvSets.Length; ++childIdx) {
			UvSetRecipe childUvSet = childUvSets[childIdx];

			int uvOffset = mergedUvs.Count;

			mergedUvs.AddRange(childUvSet.Uvs);

			mergedFaces.AddRange(childUvSet.Faces.Select(face => face.Reindex(uvOffset)));
		}
		
		return new UvSetRecipe(
			parentUvSet.Name,
			mergedUvs.ToArray(),
			mergedFaces.ToArray());
	}
}
