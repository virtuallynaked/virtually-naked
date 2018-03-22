using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class UvSetImporter {
	private readonly GeometryRecipe geometry;
	private readonly List<UvSetRecipe> recipes = new List<UvSetRecipe>();

	public UvSetImporter(GeometryRecipe geometry) {
		this.geometry = geometry;
	}

	public IEnumerable<UvSetRecipe> Recipes => recipes;

	public void ImportFrom(DsonTypes.UvSet uvSet) {
		string name = uvSet.name;

		var uvs = uvSet.uvs.values
			.Select(values => new Vector2(values[0], values[1]))
			.ToArray();

		Quad[] uvFaces = (Quad[]) geometry.Faces.Clone();
		foreach (int[] values in uvSet.polygon_vertex_indices) {
			int faceIdx = values[0];
			int vertexIdx = values[1];
			int uvIdx = values[2];

			Quad face = uvFaces[faceIdx];

			if (!face.Contains(vertexIdx)) {
				throw new InvalidOperationException("face doesn't contain vertex to override");
			}
			
			Quad replacementFace = face.Map(idx => idx == vertexIdx ? uvIdx : idx);
			uvFaces[faceIdx] = replacementFace;
		}

		var recipe = new UvSetRecipe(name, uvs, uvFaces);
		recipes.Add(recipe);
	}

	public void ImportFrom(DsonTypes.UvSet[] uvSets) {
		if (uvSets == null) {
			return;
		}

		foreach (var uvSet in uvSets) {
			ImportFrom(uvSet);
		}
	}

	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.uv_set_library);
	}

	public static IEnumerable<UvSetRecipe> ImportForFigure(DsonObjectLocator locator, FigureUris figureUris, GeometryRecipe geometry) {
		UvSetImporter importer = new UvSetImporter(geometry);
		
		foreach (DsonTypes.DsonDocument doc in locator.GetAllDocumentsUnderPath(figureUris.UvSetsBasePath)) {
			importer.ImportFrom(doc);
		}

		return importer.Recipes;
	}
}
