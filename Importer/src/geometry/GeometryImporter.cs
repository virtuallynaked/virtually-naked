using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class GeometryImporter {
	private readonly List<GeometryRecipe> recipes = new List<GeometryRecipe>();

	private static Vector3 FromArray(float[] array) {
        return new Vector3(array[0], array[1], array[2]);
    }

	public void Import(DsonTypes.Geometry geometry) {
		GeometryType type;
		if (geometry.type == DsonTypes.GeometryType.PolygonMesh) {
			type = GeometryType.PolygonMesh;
		} else if (geometry.type == DsonTypes.GeometryType.SubdivisionSurface) {
			type = GeometryType.SubdivisionSurface;
		} else {
			throw new InvalidOperationException("unrecognized geometry type: " + geometry.type);
		}

		Quad[] faces = geometry.polylist.values
			.Select(values => {
				if (values.Length == Quad.SideCount + 2) {
					return new Quad(values[2], values[3], values[4], values[5]);
				} else if (values.Length == Quad.SideCount + 2 - 1) {
					return Quad.MakeDegeneratedIntoTriangle(values[2], values[3], values[4]);
				} else {
					throw new InvalidOperationException("expected only quads and tris");
				}
			}).ToArray();

		string[] faceGroupNames = geometry.polygon_groups.values;
		
		string[] surfaceNames = geometry.polygon_material_groups.values;

		int[] faceGroupMap = geometry.polylist.values
			.Select(values => {
				return values[0];
			}).ToArray();

		int[] surfaceMap = geometry.polylist.values
			.Select(values => {
				return values[1];
			}).ToArray();

		Vector3[] vertexPositions = geometry.vertices.values
			.Select(values => FromArray(values))
			.ToArray();

		string defaultUvSet = geometry.default_uv_set.ReferencedObject.name;

		Graft graft;
		if (geometry.graft != null && (geometry.graft.hidden_polys != null || geometry.graft.vertex_pairs != null)) {
			Graft.VertexPair[] vertexPairs = geometry.graft.vertex_pairs.values
				.Select(values => new Graft.VertexPair(values[0], values[1]))
				.ToArray();
			int[] hiddenFaces = geometry.graft.hidden_polys.values;
			graft = new Graft(vertexPairs, hiddenFaces); 
		} else {
			graft = null;
		}

		recipes.Add(new GeometryRecipe(type, faces, faceGroupMap, surfaceMap, vertexPositions, faceGroupNames, surfaceNames, defaultUvSet, graft));
	}

	public void ImportFrom(DsonTypes.Geometry[] geometries) {
		if (geometries == null) {
			return;
		}

		foreach (var geometry in geometries) {
			Import(geometry);
		}
	}

	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.geometry_library);
	}

	public static GeometryRecipe ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		GeometryImporter importer = new GeometryImporter();

		importer.ImportFrom(locator.LocateRoot(figureUris.DocumentUri));

		return importer.recipes.Single();
	}
}
