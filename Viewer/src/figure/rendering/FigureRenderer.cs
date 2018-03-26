using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using Device = SharpDX.Direct3D11.Device;
using System.Collections.Generic;
using System.Linq;

public class FigureRenderer {
	public static FigureRenderer Load(IArchiveDirectory dataDir, IArchiveDirectory figureDir, Device device, ShaderCache shaderCache, string materialSetName) {
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
	
	private readonly Scatterer scatterer;
	private readonly VertexRefiner vertexRefiner;
	private readonly FigureSurface[] surfaces;
	private readonly MaterialSet materialSet;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly int[][] surfaceRenderOrderByLayer;

	public FigureRenderer(Device device, ShaderCache shaderCache, Scatterer scatterer, VertexRefiner vertexRefiner, FigureSurface[] surfaces, MaterialSet materialSet, int[][] surfaceRenderOrderByLayer) {
		this.scatterer = scatterer;
		this.vertexRefiner = vertexRefiner;
		this.surfaces = surfaces;
		this.materialSet = materialSet;
		this.surfaceRenderOrderByLayer = surfaceRenderOrderByLayer;
		
		var vertexShaderAndBytecode = shaderCache.GetVertexShader<FigureRenderer>("figure/rendering/Figure");
		this.vertexShader = vertexShaderAndBytecode;
		this.inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, vertexRefiner.RefinedVertexBufferInputElements);
	}
		
	public void Dispose() {
		scatterer?.Dispose();
		vertexRefiner.Dispose();
		foreach (var surface in surfaces) {
			surface.Dispose();
		}
		materialSet.Dispose();
		inputLayout.Dispose();
	}
		
	public void Update(DeviceContext context, ImageBasedLightingEnvironment lightingEnvironment, ShaderResourceView controlVertexInfosView) {
		scatterer?.Scatter(context, lightingEnvironment, controlVertexInfosView);
		vertexRefiner.RefineVertices(context, controlVertexInfosView, scatterer?.ScatteredIlluminationView);
	}
	
	public void RenderPass(DeviceContext context, RenderingPass pass) {
		var orderedSurfaceIdxs = surfaceRenderOrderByLayer[(int) pass.Layer];
		if (orderedSurfaceIdxs == null || orderedSurfaceIdxs.Length == 0) {
			return;
		}

		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        context.InputAssembler.SetVertexBuffers(0, vertexRefiner.RefinedVertexBufferBinding);
		context.InputAssembler.InputLayout = inputLayout;

		context.VertexShader.Set(vertexShader);
		
		foreach (int surfaceIdx in orderedSurfaceIdxs) {
			var surface = surfaces[surfaceIdx];
			var material = materialSet.Materials[surfaceIdx];

			material.Apply(context, pass);
			surface.Draw(context);
			material.Unapply(context);
		}
	}
}
