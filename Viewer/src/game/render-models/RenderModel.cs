using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using Valve.VR;
using Buffer = SharpDX.Direct3D11.Buffer;

class RenderModel {
	private readonly int VertexDataSize = Marshal.SizeOf<RenderModel_Vertex_t>();
		
	//Note that the RenderModel doesn't take ownership of its resources (they're owned by the RenderModelCache)
	private readonly VertexBufferBinding vertexBufferBinding;
	private readonly Buffer indexBuffer;
	private readonly int indexCount;
	private readonly IOpaqueMaterial material;
	
	public RenderModel(VertexBufferBinding vertexBufferBinding, Buffer indexBuffer, int indexCount, IOpaqueMaterial material) {
		this.vertexBufferBinding = vertexBufferBinding;
		this.indexBuffer = indexBuffer;
		this.indexCount = indexCount;
		this.material = material;
	}
	
	public void Render(DeviceContext context, bool depthOnly) {
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
		context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
		context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
		if (depthOnly) {
			context.PixelShader.Set(null);
		} else {
			material.Apply(context);
		}
		context.DrawIndexed(indexCount, 0, 0);
	}
}
