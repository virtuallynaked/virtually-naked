using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureOcclusionCalculator : IDisposable {
	public class Result {
		public OcclusionInfo[] FigureOcclusion { get; }
		public OcclusionInfo[] BaseFigureOcclusion { get; }

		public Result(OcclusionInfo[] figureOcclusion, OcclusionInfo[] baseFigureOcculsion) {
			FigureOcclusion = figureOcclusion;
			BaseFigureOcclusion = baseFigureOcculsion;
		}
	}

	private static float[] CalculateFaceTransparencies(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure figure, int[] surfaceMap) {
		if (figure.Name == "liv-hair") {
			var calculator = new FromTextureFaceTransparencyCalculator(fileLocator, device, shaderCache, figure);
			return calculator.CalculateSurfaceTransparencies();
		}

		var surfaceProperties = SurfacePropertiesJson.Load(figure);
		
		return surfaceMap
			.Select(surfaceIdx => 1 - surfaceProperties.Opacities[surfaceIdx])
			.ToArray();
	}

	private readonly Device device;

	private readonly FigureGroup figureGroup;

	private readonly StructuredBufferManager<Vector3> vertexPositionsBufferManager;
	private readonly BasicVertexRefiner vertexRefiner;
	private readonly GpuOcclusionCalculator occlusionCalculator;

	public FigureOcclusionCalculator(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, FigureGroup figureGroup) {
		this.device = device;
		
		this.figureGroup = figureGroup;

		var baseFigure = figureGroup.Parent;
		var figure = GroupExportUtils.FindExportTarget(figureGroup);

		var level0RefinementResult = figure.Geometry.AsTopology().Refine(0);
		float[] faceTransparencies = CalculateFaceTransparencies(fileLocator, device, shaderCache, figure, level0RefinementResult.SurfaceMap);

		if (baseFigure != figure) {
			var baseRefinementResult = baseFigure.Geometry.AsTopology().Refine(0);
			var baseFaceTransparencies = CalculateFaceTransparencies(fileLocator, device, shaderCache, baseFigure, baseRefinementResult.SurfaceMap);

			level0RefinementResult = RefinementResult.Combine(baseRefinementResult, level0RefinementResult);
			faceTransparencies = baseFaceTransparencies.Concat(faceTransparencies).ToArray();
		}

		SubdivisionMesh level0SubdivisionMesh = level0RefinementResult.Mesh;
		int[] surfaceMap = level0RefinementResult.SurfaceMap;

		this.vertexPositionsBufferManager = new StructuredBufferManager<Vector3>(device, level0SubdivisionMesh.Topology.VertexCount);

		this.vertexRefiner = new BasicVertexRefiner(device, shaderCache, level0SubdivisionMesh.Stencils);

		
		occlusionCalculator = new GpuOcclusionCalculator(device, shaderCache, level0SubdivisionMesh.Topology, faceTransparencies);
	}

	public void Dispose() {
		occlusionCalculator.Dispose();
	}
	
	public Result CalculateOcclusionInformation(ChannelOutputsGroup outputsGroup) {
		List<Vector3> controlPositions = new List<Vector3>();

		var baseFigure = figureGroup.Parent;
		var figure = GroupExportUtils.FindExportTarget(figureGroup);

		var baseOutputs = outputsGroup.ParentOutputs;
		var outputs = GroupExportUtils.FindExportTarget(outputsGroup);

		Vector3[] baseDeltas = baseFigure.CalculateDeltas(baseOutputs);
		if (baseFigure != figure) {
			controlPositions.AddRange(baseFigure.CalculateControlPositions(baseOutputs, baseDeltas));
		}

		int figureOffset = controlPositions.Count;
		
		controlPositions.AddRange(figure.CalculateControlPositions(outputs, baseDeltas));
		
		DeviceContext context = device.ImmediateContext;

		vertexPositionsBufferManager.Update(context, controlPositions.ToArray());
		ShaderResourceView vertexInfosView = vertexRefiner.Refine(context, vertexPositionsBufferManager.View);
		OcclusionInfo[] allOcclusionInfos = occlusionCalculator.Run(device.ImmediateContext, vertexInfosView);

		var figureOcclusionInfos = allOcclusionInfos.Skip(figureOffset).ToArray();
		if (figureOcclusionInfos.Length != figure.Geometry.VertexCount) {
			throw new InvalidOperationException();
		}

		OcclusionInfo[] baseFigureOcclusionInfos;
		if (baseFigure != figure) {
			baseFigureOcclusionInfos = allOcclusionInfos.Take(figureOffset).ToArray();
		} else {
			baseFigureOcclusionInfos = null;
		}

		return new Result(figureOcclusionInfos, baseFigureOcclusionInfos);
	}
}
