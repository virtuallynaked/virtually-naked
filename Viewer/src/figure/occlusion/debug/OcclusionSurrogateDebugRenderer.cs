using SharpDX;
using SharpDX.Direct3D11;
using System;

public class OcclusionSurrogateDebugRenderer : IDisposable {
	private static TriMesh SurrogateMesh = GeometricPrimitiveFactory.MakeOctahemisphere(4);
	private static TriMesh DrawMesh = GeometricPrimitiveFactory.MakeOctahemisphere(4);
	
	private readonly ShaderResourceView surrogateFacesView;
	private readonly MeshBuffers meshBuffers;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly IOpaqueMaterial material;
	
	public OcclusionSurrogateDebugRenderer(Device device, ShaderCache shaderCache, int occlusionInfoOffset) {
		surrogateFacesView = BufferUtilities.ToStructuredBufferView(device, SurrogateMesh.Faces.ToArray());

		meshBuffers = new MeshBuffers(device, DrawMesh);

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<OcclusionSurrogateDebugRenderer>("figure/occlusion/debug/DebugVertex");
		vertexShader = vertexShaderAndBytecode;
		inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, QuadMeshBuffers.InputElements);

		using (var texture = MonochromaticTextures.Make(device, Color.LightGray)) {
			var textureView = new ShaderResourceView(device, texture);
			material = new BasicSpecularMaterial(device, shaderCache, textureView);
		}
	}

	public void Dispose() {
		surrogateFacesView.Dispose();
		meshBuffers.Dispose();
		inputLayout.Dispose();
		material.Dispose();
	}
	
	private void Render(DeviceContext context, ShaderResourceView occlusionInfosView) {
		context.InputAssembler.InputLayout = inputLayout;
		meshBuffers.Bind(context.InputAssembler);

		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetShaderResources(0, surrogateFacesView, occlusionInfosView);

		material.Apply(context);

		meshBuffers.Draw(context);

		context.VertexShader.SetShaderResources(0, null, null);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass, ShaderResourceView occlusionInfosView) {
		if (pass.Layer == RenderingLayer.Opaque) {
			Render(context, occlusionInfosView);
		}
	}
}
