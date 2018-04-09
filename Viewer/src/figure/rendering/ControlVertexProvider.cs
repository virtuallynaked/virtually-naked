using SharpDX.Direct3D11;
using System;
using Device = SharpDX.Direct3D11.Device;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class ControlVertexProvider : IDisposable {
	public static readonly int ControlVertex_SizeInBytes = Vector3.SizeInBytes + OcclusionInfo.PackedSizeInBytes;

	public static ControlVertexProvider Load(Device device, ShaderCache shaderCache, FigureDefinition definition) {
		var shaperParameters = Persistance.Load<ShaperParameters>(definition.Directory.File("shaper-parameters.dat"));
		
		var occluderLoader = new OccluderLoader(device, shaderCache, definition);

		var provider = new ControlVertexProvider(
			device, shaderCache, occluderLoader,
			definition,
			shaperParameters);

		return provider;
	}
	
	private readonly OccluderLoader occluderLoader;
	private readonly FigureDefinition definition;
	private readonly GpuShaper shaper;
	private readonly int vertexCount;

	private IArchiveDirectory occlusionDirectory;
	private IOccluder occluder;
	private bool isVisible;
	
	private readonly InOutStructuredBufferManager<ControlVertexInfo> controlVertexInfosBufferManager;

	private const int BackingArrayCount = 2;
	private readonly StagingStructuredBufferManager<ControlVertexInfo> controlVertexInfoStagingBufferManager;

	public ControlVertexProvider(Device device, ShaderCache shaderCache,
		OccluderLoader occluderLoader,
		FigureDefinition definition,
		ShaperParameters shaperParameters) {
		this.occluderLoader = occluderLoader;
		this.definition = definition;
		this.shaper = new GpuShaper(device, shaderCache, definition, shaperParameters);
		this.vertexCount = shaperParameters.InitialPositions.Length;
		
		controlVertexInfosBufferManager = new InOutStructuredBufferManager<ControlVertexInfo>(device, vertexCount);
		if (definition.ChannelSystem.Parent == null) {
			this.controlVertexInfoStagingBufferManager = new StagingStructuredBufferManager<ControlVertexInfo>(device, vertexCount, BackingArrayCount);
		}
	}
	
	public void Dispose() {
		shaper.Dispose();
		occluder?.Dispose();
		controlVertexInfosBufferManager.Dispose();

		if (controlVertexInfoStagingBufferManager != null) {
			controlVertexInfoStagingBufferManager.Dispose();
		}
	}

	public int VertexCount => vertexCount;

	public ShaderResourceView ControlVertexInfosView => controlVertexInfosBufferManager.InView;

	public void SyncWithModel(FigureModel model, List<ControlVertexProvider> children) {
		//sync occluder
		var newOcclusionDirectory = model.Shape.Directory ?? occluderLoader.DefaultDirectory;

		if (newOcclusionDirectory != occlusionDirectory) {
			var newOccluder = occluderLoader.Load(newOcclusionDirectory);

			occluder?.Dispose();
			
			occlusionDirectory = newOcclusionDirectory;
			occluder = newOccluder;
		}

		//sync visible
		isVisible = model.IsVisible;

		//register child occluders
		if (children != null) {
			//children are synced first so all children should have occluders by this point
			var childOccluders = children
				.Where(child => child.isVisible)
				.Select(child => child.occluder)
				.ToList();

			occluder.SetChildOccluders(childOccluders);
		}
	}

	public ChannelOutputs UpdateFrame(DeviceContext context, ChannelOutputs parentOutputs, ChannelInputs inputs) {
		var channelOutputs = definition.ChannelSystem.Evaluate(parentOutputs, inputs);
		var boneTransforms = definition.BoneSystem.GetBoneTransforms(channelOutputs);
		if (parentOutputs != null) {
			BoneSystem.PrependChildToParentBindPoseTransforms(definition.ChildToParentBindPoseTransforms, boneTransforms);
		}
		occluder.SetValues(context, channelOutputs);
		shaper.SetValues(context, channelOutputs, boneTransforms);
		return channelOutputs;
	}

	public void UpdateVertexPositionsAndGetDeltas(DeviceContext context, UnorderedAccessView deltasOutView) {
		occluder.CalculateOcclusion(context);
		shaper.CalculatePositionsAndDeltas(
			context,
			controlVertexInfosBufferManager.OutView,
			occluder.OcclusionInfosView,
			deltasOutView);
		controlVertexInfoStagingBufferManager.CopyToStagingBuffer(context, controlVertexInfosBufferManager.Buffer);
	}

	public void UpdateVertexPositions(DeviceContext context, ShaderResourceView parentDeltasView) {
		occluder.CalculateOcclusion(context);
		shaper.CalculatePositions(
			context,
			controlVertexInfosBufferManager.OutView,
			occluder.OcclusionInfosView,
			parentDeltasView);
	}
	
	private volatile ControlVertexInfo[] previousFramePosedVertices;

	public void ReadbackPosedControlVertices(DeviceContext context) {
		if (controlVertexInfoStagingBufferManager != null) {
			previousFramePosedVertices = controlVertexInfoStagingBufferManager.FillArrayFromStagingBuffer(context);
		}
	}

	public ControlVertexInfo[] GetPreviousFrameResults(DeviceContext context) {
		return previousFramePosedVertices;
	}
}
