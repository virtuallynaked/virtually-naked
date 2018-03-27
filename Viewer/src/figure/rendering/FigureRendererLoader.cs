using Device = SharpDX.Direct3D11.Device;
using System.Collections.Generic;

public class FigureRendererLoader {
	private readonly IArchiveDirectory dataDir;
	private readonly Device device;
	private readonly ShaderCache shaderCache;

	public FigureRendererLoader(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache) {
		this.dataDir = dataDir;
		this.device = device;
		this.shaderCache = shaderCache;
	}

	public FigureRenderer Load(IArchiveDirectory figureDir, string materialSetName) {
		SurfaceProperties surfaceProperties = Persistance.Load<SurfaceProperties>(figureDir.File("surface-properties.dat"));

		var meshDirectory = figureDir.Subdirectory("refinement").Subdirectory("level-" + surfaceProperties.SubdivisionLevel);
		SubdivisionMesh mesh = SubdivisionMeshPersistance.Load(meshDirectory);
		int[] surfaceMap = meshDirectory.File("surface-map.array").ReadArray<int>();
		
		var materialSet = MaterialSet.LoadActive(device, shaderCache, dataDir, figureDir, materialSetName, surfaceProperties);
		var materials = materialSet.Materials;
		
		Scatterer scatterer = surfaceProperties.PrecomputeScattering ? Scatterer.Load(device, shaderCache, figureDir, materialSetName) : null;

		var uvSetName = materials[0].UvSet;
		IArchiveDirectory uvSetDirectory = figureDir.Subdirectory("uv-sets").Subdirectory(uvSetName);

		var texturedVertexInfos = uvSetDirectory.File("textured-vertex-infos.array").ReadArray<TexturedVertexInfo>();
		Quad[] texturedFaces = uvSetDirectory.File("textured-faces.array").ReadArray<Quad>();
		
		var vertexRefiner = new VertexRefiner(device, shaderCache, mesh, texturedVertexInfos);
		
		FigureSurface[] surfaces = FigureSurface.MakeSurfaces(device, materials.Length, texturedFaces, surfaceMap);
		
		HashSet<int> orderedSurfaces = new HashSet<int>();
		List<int> opaqueSurfaces = new List<int>();
		List<int> orderedTransparentSurfaces = new List<int>();
		List<int> unorderedTransparentSurfaces = new List<int>();
		foreach (int surfaceIdx in surfaceProperties.RenderOrder) {
			orderedSurfaces.Add(surfaceIdx);
			if (!materials[surfaceIdx].IsTransparent) {
				opaqueSurfaces.Add(surfaceIdx);
			} else {
				orderedTransparentSurfaces.Add(surfaceIdx);
			}
		}
		for (int surfaceIdx = 0; surfaceIdx < surfaces.Length; ++surfaceIdx) {
			if (orderedSurfaces.Contains(surfaceIdx)) {
				continue;
			}
			if (!materials[surfaceIdx].IsTransparent) {
				opaqueSurfaces.Add(surfaceIdx);
			} else {
				unorderedTransparentSurfaces.Add(surfaceIdx);
			}
		}
		
		int[][] surfaceRenderOrderByLayer = new int[RenderingPass.Layers.Length][];
		surfaceRenderOrderByLayer[(int) RenderingLayer.Opaque] = opaqueSurfaces.ToArray();
		surfaceRenderOrderByLayer[(int) RenderingLayer.BackToFrontTransparent] = orderedTransparentSurfaces.ToArray();
		surfaceRenderOrderByLayer[(int) RenderingLayer.UnorderedTransparent] = unorderedTransparentSurfaces.ToArray();
		
		return new FigureRenderer(device, shaderCache, scatterer, vertexRefiner, surfaces, materialSet, surfaceRenderOrderByLayer);
	}
}
