using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class VertexRefiner : IDisposable {
	private static readonly StreamOutputElement[] StreamOutputElements = new StreamOutputElement[] {
		new StreamOutputElement(0, "POSITION", 0, 0, 3, 0), //position
		new StreamOutputElement(0, "NORMAL", 0, 0, 3, 0), //normal

		new StreamOutputElement(0, "COLOR", 0, 0, 2, 0), //occlusion

		new StreamOutputElement(0, "TANGENT", 0, 0, 3, 0), //tangent
		new StreamOutputElement(0, "TEXCOORD", 0, 0, 2, 0), //texCoord

		new StreamOutputElement(0, "TANGENT", 1, 0, 3, 0), //secondaryTangent
		new StreamOutputElement(0, "TEXCOORD", 1, 0, 2, 0), //secondaryTexCoord

		new StreamOutputElement(0, "COLOR", 1, 0, 3, 0), //scatteredIllumination
	};
	private static readonly InputElement[] StreamInputElements = new[] {
        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0 * 4, 0),
		new InputElement("NORMAL", 0, Format.R32G32B32_Float, 3 * 4, 0),

		new InputElement("COLOR", 0, Format.R32G32_Float, 6 * 4, 0),
			
		new InputElement("TANGENT", 0, Format.R32G32B32_Float, 8 * 4, 0),
		new InputElement("TEXCOORD", 0, Format.R32G32_Float, 11 * 4, 0),

		new InputElement("TANGENT", 1, Format.R32G32B32_Float, 13 * 4, 0),
		new InputElement("TEXCOORD", 1, Format.R32G32_Float, 16 * 4, 0),

		new InputElement("COLOR", 1, Format.R32G32B32_Float, 18 * 4, 0),
    };
	private static readonly int StreamStride = 21 * 4;
		
	private readonly int refinedVertexCount;
	
	private readonly VertexShader vertexRefinerShader;
	private readonly GeometryShader vertexRefinerGeometryShader;

	private readonly ShaderResourceView[] shaderResources;

	private readonly Buffer refinedVertexBuffer; // stream-out | vertex
	
	public VertexRefiner(Device device, ShaderCache shaderCache, SubdivisionMesh mesh, int[] texturedToSpatialIdxMap, TexturedVertexInfo[] texturedVertexInfos) {
		this.refinedVertexCount = texturedVertexInfos.Length;

		var vertexRefinerShaderAndBytecode = shaderCache.GetVertexShader<FigureRenderer>("figure/rendering/VertexRefiner");
		this.vertexRefinerShader = vertexRefinerShaderAndBytecode;
		this.vertexRefinerGeometryShader = new GeometryShader(device, vertexRefinerShaderAndBytecode.Bytecode, StreamOutputElements, new int[] { StreamStride }, GeometryShader.StreamOutputNoRasterizedStream);

		this.shaderResources = new ShaderResourceView[] {
			BufferUtilities.ToStructuredBufferView(device, mesh.Stencils.Segments),
			BufferUtilities.ToStructuredBufferView(device, mesh.Stencils.Elems),
			BufferUtilities.ToStructuredBufferView(device, texturedToSpatialIdxMap),
			BufferUtilities.ToStructuredBufferView(device, texturedVertexInfos)
		};

		this.refinedVertexBuffer = new Buffer(device, new BufferDescription {
			SizeInBytes = refinedVertexCount * StreamStride,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.StreamOutput | BindFlags.VertexBuffer
		});		
	}
	
	public void Dispose() {
		vertexRefinerGeometryShader.Dispose();
		foreach (IDisposable disposable in shaderResources) {
			disposable.Dispose();
		}
		refinedVertexBuffer.Dispose();
	}

	public InputElement[] RefinedVertexBufferInputElements => StreamInputElements;
	public VertexBufferBinding RefinedVertexBufferBinding => new VertexBufferBinding(refinedVertexBuffer, StreamStride, 0);

	public void RefineVertices(DeviceContext context, ShaderResourceView controlVertexInfosView, ShaderResourceView secondaryTexturedVertexInfosView, ShaderResourceView scatteredIlluminationView) {
		context.WithEvent("VertexRefiner::RefineVertices", () => {
			context.ClearState();

			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

			context.VertexShader.SetShaderResources(0, shaderResources);
			context.VertexShader.SetShaderResources(shaderResources.Length,
				secondaryTexturedVertexInfosView,
				controlVertexInfosView,
				scatteredIlluminationView);
			context.VertexShader.Set(vertexRefinerShader);
		
			context.GeometryShader.Set(vertexRefinerGeometryShader);

			context.StreamOutput.SetTarget(refinedVertexBuffer, 0);

			context.Draw(refinedVertexCount, 0);
		
			context.ClearState();
		});
	}
}
