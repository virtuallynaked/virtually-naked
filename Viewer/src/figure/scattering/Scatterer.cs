using System;
using SharpDX.Direct3D11;
using SharpDX;

public class Scatterer : IDisposable {
	public const string FormFactorSegmentsFilename = "scattering-form-factor-segments.array";
	public const string FormFactoryElementsFilename = "scattering-form-factor-elements.array";

	private const int ShaderNumThreads = 64;

	public static Scatterer Load(Device device, ShaderCache shaderCache, IArchiveDirectory figureDir, String materialSetName) {
		var l0GeometryDir = figureDir.Subdirectory("refinement").Subdirectory("level-0");
		SubdivisionMesh level0Mesh = SubdivisionMeshPersistance.Load(l0GeometryDir);
		
		var scatteringDir = figureDir.Subdirectory("scattering").Subdirectory(materialSetName);
		var formFactorSegments = scatteringDir.File(FormFactorSegmentsFilename).ReadArray<ArraySegment>();
		var formFactorElements = scatteringDir.File(FormFactoryElementsFilename).ReadArray<Vector3WeightedIndex>();
		var formFactors = new PackedLists<Vector3WeightedIndex>(formFactorSegments, formFactorElements);
		
		return new Scatterer(device, shaderCache, level0Mesh, formFactors);
	}
	
	private readonly int vertexCount;
	private readonly ShaderResourceView stencilSegments;
	private readonly ShaderResourceView stencilElems;
	private readonly ShaderResourceView formFactorSegments;
	private readonly ShaderResourceView formFactorElements;

	private readonly ComputeShader samplingShader;
	private readonly ComputeShader scatteringShader;
	
	private readonly InOutStructuredBufferManager<Vector3> sampledIrrandiancesBufferManager;
	private readonly InOutStructuredBufferManager<Vector3> scatteredIrrandiancesBufferManager;

	public Scatterer(Device device, ShaderCache shaderCache, SubdivisionMesh mesh, PackedLists<Vector3WeightedIndex> formFactors) {
		vertexCount = mesh.Stencils.Count;
		stencilSegments = BufferUtilities.ToStructuredBufferView(device, mesh.Stencils.Segments);
		stencilElems = BufferUtilities.ToStructuredBufferView(device, mesh.Stencils.Elems);
		formFactorSegments = BufferUtilities.ToStructuredBufferView(device, formFactors.Segments);
		formFactorElements = BufferUtilities.ToStructuredBufferView(device, formFactors.Elems);
		
		samplingShader = shaderCache.GetComputeShader<Scatterer>("figure/scattering/SampleVertexIrradiances");
		scatteringShader = shaderCache.GetComputeShader<Scatterer>("figure/scattering/ScatterIrradiances");

		sampledIrrandiancesBufferManager = new InOutStructuredBufferManager<Vector3>(device, vertexCount);
		scatteredIrrandiancesBufferManager = new InOutStructuredBufferManager<Vector3>(device, vertexCount);
	}

	public ShaderResourceView ScatteredIlluminationView => scatteredIrrandiancesBufferManager.InView;
	
	public void Dispose() {
		stencilSegments.Dispose();
		stencilElems.Dispose();
		formFactorSegments.Dispose();
		formFactorElements.Dispose();
		
		sampledIrrandiancesBufferManager.Dispose();
		scatteredIrrandiancesBufferManager.Dispose();
	}

	public void Scatter(DeviceContext context, ImageBasedLightingEnvironment lightingEnvironment, ShaderResourceView controlVertexInfosView) {
		context.WithEvent("Scatterer::Scatter", () => {
			context.ClearState();

			context.ComputeShader.Set(samplingShader);
			lightingEnvironment.Apply(context.ComputeShader);
			context.ComputeShader.SetShaderResources(ShaderSlots.MaterialTextureStart,
				stencilSegments,
				stencilElems,
				controlVertexInfosView);
			context.ComputeShader.SetUnorderedAccessView(0, sampledIrrandiancesBufferManager.OutView);
			context.Dispatch(IntegerUtils.RoundUp(vertexCount, ShaderNumThreads), 1, 1);

			context.ClearState();

			context.ComputeShader.Set(scatteringShader);
			context.ComputeShader.SetShaderResources(0,
				sampledIrrandiancesBufferManager.InView,
				formFactorSegments,
				formFactorElements);
			context.ComputeShader.SetUnorderedAccessView(0, scatteredIrrandiancesBufferManager.OutView);
			context.Dispatch(IntegerUtils.RoundUp(vertexCount, ShaderNumThreads), 1, 1);

			context.ClearState();
		});
	}
}
