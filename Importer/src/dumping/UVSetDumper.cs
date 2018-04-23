using OpenSubdivFacade;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UVSetDumper {
	public static void DumpFigure(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo figureDestDir) {
		DirectoryInfo refinementDirectory = figureDestDir.Subdirectory("refinement").Subdirectory("level-" + surfaceProperties.SubdivisionLevel);
		Quad[] spatialFaces = refinementDirectory.File("faces.array").ReadArray<Quad>();
		SubdivisionTopologyInfo spatialTopologyInfo = Persistance.Load<SubdivisionTopologyInfo>(UnpackedArchiveFile.Make(refinementDirectory.File("topology-info.dat")));
		
		DirectoryInfo uvSetsDirectory = figureDestDir.Subdirectory("uv-sets");
		UVSetDumper dumper = new UVSetDumper(figure, surfaceProperties, uvSetsDirectory, spatialFaces, spatialTopologyInfo);
		foreach (var pair in figure.UvSets) {
			dumper.Dump(pair.Key, pair.Value);
		}
	}
	
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly DirectoryInfo uvSetsDirectory;
	private readonly Quad[] spatialFaces;
	private readonly SubdivisionTopologyInfo spatialTopologyInfo;

	public UVSetDumper(
		Figure figure,
		SurfaceProperties surfaceProperties,
		DirectoryInfo uvSetsDirectory,
		Quad[] spatialFaces, SubdivisionTopologyInfo spatialTopologyInfo) {
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
		this.uvSetsDirectory = uvSetsDirectory;
		this.spatialFaces = spatialFaces;
		this.spatialTopologyInfo = spatialTopologyInfo;
	}
		
	private MultisurfaceQuadTopology ExtractTexturedTopology(MultisurfaceQuadTopology spatialTopology, UvSet uvSet) {
		Quad[] texuredFaces = uvSet.Faces;
		int texturedVertexCount = uvSet.Uvs.Length;
		
		return new MultisurfaceQuadTopology(
			spatialTopology.Type,
			texturedVertexCount,
			spatialTopology.SurfaceCount,
			texuredFaces,
			spatialTopology.SurfaceMap);
	}
	
	public static int[] CalculateTextureToSpatialIndexMap(QuadTopology texturedTopology, Quad[] spatialFaces) {
		int faceCount = texturedTopology.Faces.Length;
		if (spatialFaces.Length != faceCount) {
			throw new InvalidOperationException("textured and spatial face count mismatch");
		}

		int[] spatialIdxMap = new int[texturedTopology.VertexCount];
		for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
			Quad texturedFace = texturedTopology.Faces[faceIdx];
			Quad spatialFace = spatialFaces[faceIdx];
			
			for (int i = 0; i < Quad.SideCount; ++i) {
				int texturedIdx = texturedFace.GetCorner(i);
				int spatialIdx = spatialFace.GetCorner(i);

				int previousMapping = spatialIdxMap[texturedIdx];
				if (previousMapping != default(int) && previousMapping != spatialIdx) {
					throw new InvalidOperationException("mapping conflict");
				}

				spatialIdxMap[texturedIdx] = spatialIdx;
			}
		}

		return spatialIdxMap;
	}
	
	private static Tuple<Vector2, Vector2> RemapTangents(
		List<int> spatialNeighbours, VertexRule spatialVertexRule,
		List<int> neighbours, VertexRule vertexRule,
		Vector2 ds, Vector2 dt) {
		int spatialValence = spatialNeighbours.Count;
		int valence = neighbours.Count;

		if (spatialVertexRule == vertexRule && spatialNeighbours.SequenceEqual(neighbours)) {
			//no remapping required
			return Tuple.Create(ds, dt);
		}
		
		if (neighbours.Count == 0) {
			//disconnected vertex, so no tangents
			return Tuple.Create(Vector2.Zero, Vector2.Zero);
		}

		
		/*
		if (vertexRule != VertexRule.Crease) {
			throw new InvalidOperationException("only texture creases should need remapping");
		}
		
		if (spatialValence <= 2) {
			throw new InvalidOperationException("spatial corners should never remapping");
		}
		*/

		if (spatialValence == 4 && spatialVertexRule == VertexRule.Smooth && valence == 3) {
			int leadingNeighbourSpatialIdx = spatialNeighbours.IndexOf(neighbours[0]);

			if (leadingNeighbourSpatialIdx == 0) {
				return Tuple.Create(ds, dt);
			} else if (leadingNeighbourSpatialIdx == 1) {
				return Tuple.Create(-dt, ds);
			} else if (leadingNeighbourSpatialIdx == 2) {
				return Tuple.Create(-ds, -dt);
			} else if (leadingNeighbourSpatialIdx == 3) {
				return Tuple.Create(dt, -ds);
			} else {
				throw new InvalidOperationException("impossible");
			}
		} else {
			return Tuple.Create(Vector2.Zero, Vector2.Zero);
		}
	}
	
	public void Dump(string name, UvSet uvSet) {
		DirectoryInfo uvSetDirectory = uvSetsDirectory.Subdirectory(name);
		if (uvSetDirectory.Exists) {
			return;
		}

		Console.WriteLine($"Dumping uv-set {name}...");
		
		MultisurfaceQuadTopology spatialTopology = figure.Geometry.AsTopology();
		MultisurfaceQuadTopology texturedControlTopology = ExtractTexturedTopology(spatialTopology, uvSet);
		var texturedRefinementResult = texturedControlTopology.Refine(surfaceProperties.SubdivisionLevel);
		var texturedTopology = texturedRefinementResult.Mesh.Topology;
		var texturedTopologyInfo = texturedRefinementResult.TopologyInfo;
		var derivStencils = texturedRefinementResult.Mesh.Stencils;
		
		var stencils = derivStencils.Map(stencil => new WeightedIndex(stencil.Index, stencil.Weight));
		var duStencils = derivStencils.Map(stencil => new WeightedIndex(stencil.Index, stencil.DuWeight));
		var dvStencils = derivStencils.Map(stencil => new WeightedIndex(stencil.Index, stencil.DvWeight));

		Vector2[] controlTextureCoords = uvSet.Uvs;
		Vector2[] textureCoords = new Subdivider(stencils).Refine(controlTextureCoords, new Vector2Operators());
		Vector2[] textureCoordDus = new Subdivider(duStencils).Refine(controlTextureCoords, new Vector2Operators());
		Vector2[] textureCoordDvs = new Subdivider(dvStencils).Refine(controlTextureCoords, new Vector2Operators());
		
		int[] spatialIdxMap = CalculateTextureToSpatialIndexMap(texturedTopology, spatialFaces);

		TexturedVertexInfo[] texturedVertexInfos = Enumerable.Range(0, textureCoords.Length)
			.Select(idx => {
				int spatialVertexIdx = spatialIdxMap[idx];
				Vector2 textureCoord = textureCoords[idx];
				Vector2 du = textureCoordDus[idx];
				Vector2 dv = textureCoordDvs[idx];

				List<int> spatialNeighbours = spatialTopologyInfo.AdjacentVertices.GetElements(spatialVertexIdx).ToList();
				var spatialVertexRule = spatialTopologyInfo.VertexRules[spatialVertexIdx];

				List<int> neighbours = texturedTopologyInfo.AdjacentVertices.GetElements(idx).Select(i => spatialIdxMap[i]).ToList();
				var vertexRule = texturedTopologyInfo.VertexRules[idx];

				Tuple<Vector2, Vector2> remappedTangents = RemapTangents(spatialNeighbours, spatialVertexRule, neighbours, vertexRule, du, dv);
				
				return new TexturedVertexInfo(
					spatialVertexIdx,
					textureCoord,
					remappedTangents.Item1,
					remappedTangents.Item2);
			})
			.ToArray();

		uvSetDirectory.CreateWithParents();
		uvSetDirectory.File("textured-faces.array").WriteArray(texturedTopology.Faces);
		uvSetDirectory.File("textured-vertex-infos.array").WriteArray(texturedVertexInfos);
	}
}
