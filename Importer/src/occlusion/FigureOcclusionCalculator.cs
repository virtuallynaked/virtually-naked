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

	private readonly StructuredBufferManager<Vector3> groupControlPositionsBufferManager;
	private readonly BasicVertexRefiner vertexRefiner;
	private readonly InOutStructuredBufferManager<BasicRefinedVertexInfo> refinedVertexInfosBufferManager;
	private readonly GpuOcclusionCalculator occlusionCalculator;

	private readonly ArraySegment parentSegment;
	private readonly List<ArraySegment> surrogateSegments = new List<ArraySegment>();

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

			var surrogates = figure.OcclusionBinding.Surrogates;
			surrogateSegments = surrogates.Select(geometryConcatenator.Add).ToList();
		}
		
		//children
		foreach (var figure in figureGroup.Children) {
			var refinementResult = figure.Geometry.AsTopology().Refine(0);
			float[] faceTransparencies = FaceTransparencyCalculator.Calculate(fileLocator, device, shaderCache, figure, refinementResult.SurfaceMap);

			var segment = geometryConcatenator.Add(refinementResult.Mesh, faceTransparencies);
			childSegments.Add(segment);

			var surrogates = figure.OcclusionBinding.Surrogates;
			if (surrogates.Count != 0) {
				// There's no technical reason this couldn't be implemented; I just haven't needed it yet.
				throw new NotImplementedException("occlusion surrogates aren't supported on child figures");
			}
		}
		
		groupControlPositionsBufferManager = new StructuredBufferManager<Vector3>(device, geometryConcatenator.Mesh.ControlVertexCount);
		vertexRefiner = new BasicVertexRefiner(device, shaderCache, geometryConcatenator.Mesh.Stencils);
		refinedVertexInfosBufferManager = new InOutStructuredBufferManager<BasicRefinedVertexInfo>(device, vertexRefiner.RefinedVertexCount);
		occlusionCalculator = new GpuOcclusionCalculator(device, shaderCache,
			geometryConcatenator.Mesh.Topology,
			geometryConcatenator.FaceTransparencies,
			geometryConcatenator.FaceMasks,
			geometryConcatenator.VertexMasks);
	}

	public void Dispose() {
		groupControlPositionsBufferManager.Dispose();
		vertexRefiner.Dispose();
		refinedVertexInfosBufferManager.Dispose();
		occlusionCalculator.Dispose();
	}
	
	public Result CalculateOcclusionInformation(ChannelOutputsGroup outputsGroup) {
		List<Vector3> groupControlPositions = new List<Vector3>();

		Vector3[] parentDeltas = figureGroup.Parent.CalculateDeltas(outputsGroup.ParentOutputs);

		List<BasicRefinedVertexInfo[]> surrogateVertexInfos = new List<BasicRefinedVertexInfo[]>();

		//parent
		{
			var figure = figureGroup.Parent;
			var outputs = outputsGroup.ParentOutputs;

			var controlPositions = figure.CalculateControlPositions(outputs, parentDeltas);
			groupControlPositions.AddRange(controlPositions);

			foreach (var surrogate in figure.OcclusionBinding.Surrogates) {
				surrogateVertexInfos.Add(surrogate.GetVertexInfos(outputs, controlPositions));
			}
		}

		int figureOffset = groupControlPositions.Count;

		//children
		for (int childIdx = 0; childIdx < figureGroup.Children.Length; ++childIdx) {
			var figure = figureGroup.Children[childIdx];
			var outputs = outputsGroup.ChildOutputs[childIdx];

			var controlPositions = figure.CalculateControlPositions(outputs, parentDeltas);
			groupControlPositions.AddRange(controlPositions);
		}
		
		DeviceContext context = device.ImmediateContext;

		groupControlPositionsBufferManager.Update(context, groupControlPositions.ToArray());
		vertexRefiner.Refine(context, groupControlPositionsBufferManager.View, refinedVertexInfosBufferManager.OutView);

		for (int surrogateIdx = 0; surrogateIdx < figureGroup.Parent.OcclusionBinding.Surrogates.Count; ++surrogateIdx) {
			var segment = surrogateSegments[surrogateIdx];
			var vertexInfos = surrogateVertexInfos[surrogateIdx];
			refinedVertexInfosBufferManager.Update(context, vertexInfos, segment.Offset);
		}
		
		OcclusionInfo[] groupOcclusionInfos = occlusionCalculator.Run(context, refinedVertexInfosBufferManager.InView);

		OcclusionInfo[] parentOcclusionInfos;
		List<OcclusionInfo[]> childOcclusionInfos = new List<OcclusionInfo[]>();
		
		//parent
		{
			var segment = parentSegment;
			
			var mainOcclusionInfos = groupOcclusionInfos.Skip(segment.Offset).Take(segment.Count);
			var surrogateOcclusionInfos = surrogateSegments
				.SelectMany(surrogateSegment => groupOcclusionInfos.Skip(surrogateSegment.Offset).Take(surrogateSegment.Count));
			
			var figureOcclusionInfos = mainOcclusionInfos.Concat(surrogateOcclusionInfos).ToArray();
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
