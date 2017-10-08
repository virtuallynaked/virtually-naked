using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureOcclusionCalculator : IDisposable {
	public class Result {
		public OcclusionInfo[] ParentOcclusion { get; }
		public List<OcclusionInfo[]> ChildOcclusions { get; }

		public Result(OcclusionInfo[] parentOcclusion, List<OcclusionInfo[]> childOcclusions) {
			ParentOcclusion = parentOcclusion;
			ChildOcclusions = childOcclusions;
		}
	}
	
	private readonly Device device;

	private readonly FigureGroup figureGroup;

	private readonly StructuredBufferManager<Vector3> controlPositionsBufferManager;
	private readonly BasicVertexRefiner vertexRefiner;
	private readonly GpuOcclusionCalculator occlusionCalculator;

	private readonly ArraySegment parentSegment;
	private readonly List<ArraySegment> childSegments = new List<ArraySegment>();
	
	public FigureOcclusionCalculator(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, FigureGroup figureGroup) {
		this.device = device;
		
		this.figureGroup = figureGroup;
		
		var geometryConcatenator = new OcclusionGeometryConcatenator();

		//parent
		{
			var figure = figureGroup.Parent;

			var refinementResult = figure.Geometry.AsTopology().Refine(0);
			var faceTransparencies = FaceTransparencyCalculator.Calculate(fileLocator, device, shaderCache, figure, refinementResult.SurfaceMap);

			var segment = geometryConcatenator.Add(refinementResult.Mesh, faceTransparencies);
			parentSegment = segment;
		}
		
		//children
		foreach (var figure in figureGroup.Children) {
			var refinementResult = figure.Geometry.AsTopology().Refine(0);
			float[] faceTransparencies = FaceTransparencyCalculator.Calculate(fileLocator, device, shaderCache, figure, refinementResult.SurfaceMap);

			var segment = geometryConcatenator.Add(refinementResult.Mesh, faceTransparencies);
			childSegments.Add(segment);
		}
		
		controlPositionsBufferManager = new StructuredBufferManager<Vector3>(device, geometryConcatenator.Mesh.Topology.VertexCount);
		vertexRefiner = new BasicVertexRefiner(device, shaderCache, geometryConcatenator.Mesh.Stencils);
		occlusionCalculator = new GpuOcclusionCalculator(device, shaderCache, geometryConcatenator.Mesh.Topology, geometryConcatenator.FaceTransparencies);
	}

	public void Dispose() {
		controlPositionsBufferManager.Dispose();
		vertexRefiner.Dispose();
		occlusionCalculator.Dispose();
	}
	
	public Result CalculateOcclusionInformation(ChannelOutputsGroup outputsGroup) {
		List<Vector3> controlPositions = new List<Vector3>();

		Vector3[] parentDeltas = figureGroup.Parent.CalculateDeltas(outputsGroup.ParentOutputs);

		//parent
		{
			var figure = figureGroup.Parent;
			var outputs = outputsGroup.ParentOutputs;

			controlPositions.AddRange(figure.CalculateControlPositions(outputs, parentDeltas));
		}

		int figureOffset = controlPositions.Count;

		//children
		for (int childIdx = 0; childIdx < figureGroup.Children.Length; ++childIdx) {
			var figure = figureGroup.Children[childIdx];
			var outputs = outputsGroup.ChildOutputs[childIdx];

			controlPositions.AddRange(figure.CalculateControlPositions(outputs, parentDeltas));
		}
		
		DeviceContext context = device.ImmediateContext;

		controlPositionsBufferManager.Update(context, controlPositions.ToArray());
		ShaderResourceView vertexInfosView = vertexRefiner.Refine(context, controlPositionsBufferManager.View);
		OcclusionInfo[] groupOcclusionInfos = occlusionCalculator.Run(device.ImmediateContext, vertexInfosView);

		OcclusionInfo[] parentOcclusionInfos;
		List<OcclusionInfo[]> childOcclusionInfos = new List<OcclusionInfo[]>();
		
		//parent
		{
			var segment = parentSegment;
			var figureOcclusionInfos = groupOcclusionInfos.Skip(segment.Offset).Take(segment.Count).ToArray();
			parentOcclusionInfos = figureOcclusionInfos;
		}

		//children
		foreach (var segment in childSegments) {
			var figureOcclusionInfos = groupOcclusionInfos.Skip(segment.Offset).Take(segment.Count).ToArray();
			childOcclusionInfos.Add(figureOcclusionInfos);
		}
		
		return new Result(parentOcclusionInfos, childOcclusionInfos);
	}
}
