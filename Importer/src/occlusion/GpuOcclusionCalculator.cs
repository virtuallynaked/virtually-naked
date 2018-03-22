using SharpDX;
using SharpDX.Direct3D11;
using System;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class GpuOcclusionCalculator : IDisposable {
	private const int RasterDim = 32;
	private const int RasterRowCount = RasterDim;
	private const int RasterRowSizeInBytes = sizeof(UInt32);
	private const int RasterSizeInBytes = RasterRowSizeInBytes * RasterRowCount;
		
	private const int RasterTableFaceCount = 3;
	private const int RasterTableDim = 128;
	private const int RasterTableElementCount = RasterTableFaceCount * RasterTableDim * RasterTableDim;

	private const int BuildTableShaderNumThreads = 64;
	private const int ShaderNumThreads = 32;

	private const int BatchSize = 256; //chosen to be as small as possible while still having minimal performance impact
	
	private readonly ComputeShader shader;

	private ShaderResourceView rasterCacheView;

	private Buffer hemispherePointsAndWeightsConstantBuffer;
	private ConstantBufferManager<ArraySegment> segmentBufferManager;

	private ShaderResourceView facesView;
	private ShaderResourceView transparenciesView;
	private ShaderResourceView faceMasksView;
	private ShaderResourceView vertexMasksView;

	private StageableStructuredBufferManager<OcclusionInfo> outputBufferManager;

	private int vertexCount;
	
	public GpuOcclusionCalculator(Device device, ShaderCache shaderCache, QuadTopology topology, float[] faceTransparencies,
		uint[] faceMasks, uint[] vertexMasks) {
		if (topology.Faces.Length != faceTransparencies.Length) {
			throw new ArgumentException("face count mismatch");
		}
		if (topology.Faces.Length != faceMasks.Length) {
			throw new ArgumentException("face count mismatch");
		}

		if (topology.VertexCount != vertexMasks.Length) {
			throw new ArgumentException("vertex count mismatch");
		}

		shader = shaderCache.GetComputeShader<GpuOcclusionCalculator>("occlusion/HemisphericalRasterizingComputeShader");
		segmentBufferManager = new ConstantBufferManager<ArraySegment>(device);
		SetupHemispherePointsAndWeights(device);
		SetupRasterTable(device, shaderCache);
		SetupTopologyBuffers(device, topology, faceTransparencies);

		faceMasksView = BufferUtilities.ToStructuredBufferView(device, faceMasks);
		vertexMasksView = BufferUtilities.ToStructuredBufferView(device, vertexMasks);
	}

	private static Vector4[] CalculateHemispherePointsAndWeights() {
		Binner binner = new Binner(RasterDim, Binner.Mode.Midpoints);

		Vector4[] pointsAndWeights = new Vector4[RasterDim * RasterDim];
		for (int row = 0; row < RasterDim; ++row) {
			for (int col = 0; col < RasterDim; ++col) {
				int idx = row * RasterDim + col;

				float x = binner.IdxToFloat(col) * 2 - 1;
				float y = -(binner.IdxToFloat(row) * 2 - 1);
				float zSquared = 1 - x * x - y * y;
				if (zSquared < 0) {
					pointsAndWeights[idx] = Vector4.Zero;
				} else {
					float z = (float) -Math.Sqrt(zSquared);
					pointsAndWeights[idx] = new Vector4(x, y, z, 1);
				}
			}
		}
		return pointsAndWeights;
	}

	private void SetupHemispherePointsAndWeights(Device device) {
		Vector4[] hemispherePointsAndWeights = CalculateHemispherePointsAndWeights();
		hemispherePointsAndWeightsConstantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, hemispherePointsAndWeights, usage: ResourceUsage.Immutable);
	}

	private void SetupRasterTable(Device device, ShaderCache shaderCache) {
		DeviceContext context = device.ImmediateContext;

		BufferDescription desc = new BufferDescription {
			SizeInBytes = RasterTableElementCount * RasterSizeInBytes,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
			OptionFlags = ResourceOptionFlags.BufferStructured,
			StructureByteStride = RasterSizeInBytes
		};

		var buildTableShader = shaderCache.GetComputeShader<GpuOcclusionCalculator>("occlusion/BuildHemisphericalRasterizingTable");

		using (Buffer buffer = new Buffer(device, desc))
		using (UnorderedAccessView view = new UnorderedAccessView(device, buffer)) {
			context.ComputeShader.Set(buildTableShader);
			context.ComputeShader.SetUnorderedAccessView(0, view);
			context.ComputeShader.SetConstantBuffer(0, hemispherePointsAndWeightsConstantBuffer);
			context.Dispatch(RasterTableDim / BuildTableShaderNumThreads, RasterTableDim, RasterTableFaceCount);
			context.ClearState();

			rasterCacheView = new ShaderResourceView(device, buffer);
		}
	}

	private void SetupTopologyBuffers(Device device, QuadTopology topology, float[] faceTransparencies) {
		vertexCount = topology.VertexCount;

		facesView = BufferUtilities.ToStructuredBufferView(device, topology.Faces);
		transparenciesView = BufferUtilities.ToStructuredBufferView(device, faceTransparencies);
		
		outputBufferManager = new StageableStructuredBufferManager<OcclusionInfo>(device, vertexCount);
	}

	public void Dispose() {
		hemispherePointsAndWeightsConstantBuffer.Dispose();
		segmentBufferManager.Dispose();

		rasterCacheView.Dispose();
		
		facesView.Dispose();
		transparenciesView.Dispose();

		outputBufferManager.Dispose();

		vertexMasksView.Dispose();
		faceMasksView.Dispose();
	}
	
	public OcclusionInfo[] Run(DeviceContext context, ShaderResourceView vertexInfos) {
		context.ClearState();
		context.ComputeShader.Set(shader);
		context.ComputeShader.SetConstantBuffer(0, hemispherePointsAndWeightsConstantBuffer);
		context.ComputeShader.SetShaderResources(0,
			rasterCacheView,
			facesView,
			transparenciesView,
			vertexMasksView,
			faceMasksView,
			vertexInfos);
		context.ComputeShader.SetUnorderedAccessView(0, outputBufferManager.View);

		for (int baseVertexIdx = 0; baseVertexIdx < vertexCount; baseVertexIdx += BatchSize) {
			ArraySegment segment = new ArraySegment(baseVertexIdx, Math.Max(vertexCount - baseVertexIdx, BatchSize));
			segmentBufferManager.Update(context, segment);
			context.ComputeShader.SetConstantBuffer(1, segmentBufferManager.Buffer);
			context.Dispatch(1, IntegerUtils.RoundUp(BatchSize, ShaderNumThreads / RasterRowCount), 1);
			context.Flush();
		}
		
		return outputBufferManager.ReadContents(context);
	}
}
