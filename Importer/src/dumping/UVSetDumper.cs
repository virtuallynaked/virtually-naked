using OpenSubdivFacade;
using SharpDX;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class UVSetDumper {
	public static void DumpFigure(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo figureDestDir) {
		DirectoryInfo uvSetsDirectory = figureDestDir.Subdirectory("uv-sets");
		UVSetDumper dumper = new UVSetDumper(figure, surfaceProperties, uvSetsDirectory);
		dumper.DumpShared();
		foreach (var pair in figure.UvSets) {
			dumper.Dump(pair.Key, pair.Value);
		}
	}
	
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly DirectoryInfo uvSetsDirectory;

	public UVSetDumper(
		Figure figure,
		SurfaceProperties surfaceProperties,
		DirectoryInfo uvSetsDirectory) {
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
		this.uvSetsDirectory = uvSetsDirectory;
	}
	
	public void DumpShared() {
		var texturedFacesFile = uvSetsDirectory.File("textured-faces.array");
		var textureToSpatialIdxMapFile = uvSetsDirectory.File("textured-to-spatial-idx-map.array");
		if (texturedFacesFile.Exists && textureToSpatialIdxMapFile.Exists) {
			return;
		}
		
		int subdivisionLevel = surfaceProperties.SubdivisionLevel;

		var geometry = figure.Geometry;
		var spatialControlTopology = new QuadTopology(geometry.VertexCount, geometry.Faces);
		QuadTopology spatialTopology;
		using (var refinement = new Refinement(spatialControlTopology, subdivisionLevel)) {
			spatialTopology = refinement.GetTopology();
		}

		var uvSet = figure.DefaultUvSet;
		var texturedControlTopology = new QuadTopology(uvSet.Uvs.Length, uvSet.Faces);
		QuadTopology texturedTopology;
		using (var refinement = new Refinement(texturedControlTopology, surfaceProperties.SubdivisionLevel, BoundaryInterpolation.EdgeAndCorner)) {
			texturedTopology = refinement.GetTopology();
		}

		int[] texturedToSpatialIndexMap = QuadTopology.CalculateVertexIndexMap(texturedTopology, spatialTopology.Faces);

		uvSetsDirectory.CreateWithParents();
		texturedFacesFile.WriteArray(texturedTopology.Faces);
		textureToSpatialIdxMapFile.WriteArray(texturedToSpatialIndexMap);
	}
	
	private UvSet RemapToDefault(UvSet uvSet) {
		var defaultUvSet = figure.DefaultUvSet;
		var defaultUvSetTopology = new QuadTopology(defaultUvSet.Uvs.Length, defaultUvSet.Faces);
		var indexMap = QuadTopology.CalculateVertexIndexMap(defaultUvSetTopology, uvSet.Faces);

		Vector2[] remappedUvs = indexMap
			.Select(idx => uvSet.Uvs[idx])
			.ToArray();

		return new UvSet(uvSet.Name, remappedUvs, defaultUvSet.Faces);
	}

		
	public void Dump(string name, UvSet uvSet) {
		DirectoryInfo uvSetDirectory = uvSetsDirectory.Subdirectory(name);
		if (uvSetDirectory.Exists) {
			return;
		}

		Console.WriteLine($"Dumping uv-set {name}...");
		
		int subdivisionLevel = surfaceProperties.SubdivisionLevel;

		var geometry = figure.Geometry;
		var spatialControlTopology = new QuadTopology(geometry.VertexCount, geometry.Faces);
		var spatialControlPositions = geometry.VertexPositions;

		QuadTopology spatialTopology;
		LimitValues<Vector3> spatialLimitPositions;
		using (var refinement = new Refinement(spatialControlTopology, subdivisionLevel)) {
			spatialTopology = refinement.GetTopology();
			spatialLimitPositions = refinement.LimitFully(spatialControlPositions);
		}
		
		uvSet = RemapToDefault(uvSet);

		var texturedControlTopology = new QuadTopology(uvSet.Uvs.Length, uvSet.Faces);
		Vector2[] controlTextureCoords = uvSet.Uvs;

		int[] controlSpatialIdxMap = QuadTopology.CalculateVertexIndexMap(texturedControlTopology, spatialControlTopology.Faces);
		Vector3[] texturedControlPositions = controlSpatialIdxMap
			.Select(spatialIdx => spatialControlPositions[spatialIdx])
			.ToArray();

		QuadTopology texturedTopology;
		LimitValues<Vector3> texturedLimitPositions;
		LimitValues<Vector2> limitTextureCoords;
		using (var refinement = new Refinement(texturedControlTopology, surfaceProperties.SubdivisionLevel, BoundaryInterpolation.EdgeAndCorner)) {
			texturedTopology = refinement.GetTopology();
			texturedLimitPositions = refinement.LimitFully(texturedControlPositions);
			limitTextureCoords = refinement.LimitFully(controlTextureCoords);
		}

		Vector2[] textureCoords;
		if (geometry.Type == GeometryType.SubdivisionSurface) {
			textureCoords = limitTextureCoords.values;
		} else {
			if (subdivisionLevel != 0) {
				throw new InvalidOperationException("polygon meshes cannot be subdivided");
			}
			Debug.Assert(limitTextureCoords.values.Length == controlTextureCoords.Length);

			textureCoords = controlTextureCoords;
		}
				
		int[] spatialIdxMap = QuadTopology.CalculateVertexIndexMap(texturedTopology, spatialTopology.Faces);

		TexturedVertexInfo[] texturedVertexInfos = Enumerable.Range(0, textureCoords.Length)
			.Select(idx => {
				int spatialVertexIdx = spatialIdxMap[idx];
				Vector2 textureCoord = textureCoords[idx];

				Vector3 positionDu = TangentSpaceUtilities.CalculatePositionDu(
					limitTextureCoords.tangents1[idx],
					limitTextureCoords.tangents2[idx],
					texturedLimitPositions.tangents1[idx],
					texturedLimitPositions.tangents2[idx]);
				
				Vector3 spatialPositionTan1 = spatialLimitPositions.tangents1[spatialVertexIdx];
				Vector3 spatialPositionTan2 = spatialLimitPositions.tangents2[spatialVertexIdx];
				
				Vector2 tangentUCoeffs = TangentSpaceUtilities.CalculateTangentSpaceRemappingCoeffs(spatialPositionTan1, spatialPositionTan2, positionDu);
				
				DebugUtilities.AssertFinite(tangentUCoeffs.X);
				DebugUtilities.AssertFinite(tangentUCoeffs.Y);

				return new TexturedVertexInfo(
					textureCoord,
					tangentUCoeffs);
			})
			.ToArray();

		uvSetDirectory.CreateWithParents();
		uvSetDirectory.File("textured-vertex-infos.array").WriteArray(texturedVertexInfos);
	}
}
