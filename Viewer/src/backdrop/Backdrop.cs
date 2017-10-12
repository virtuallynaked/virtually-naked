using SharpDX;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

class Backdrop {
	private readonly MeshBuffers meshBuffers;
	private readonly CoordinateNormalMatrixPairConstantBufferManager modelToWorldTransform;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly PixelShader pixelShader;

	private readonly Matrix transform;

	public Backdrop(Device device, ShaderCache shaderCache) {
		float size = 5f;
		Matrix transform = Matrix.Translation(0, size / 2, 0);
		QuadMesh mesh = GeometricPrimitiveFactory.MakeCube(size).Flip();

		this.meshBuffers = new MeshBuffers(device, mesh.AsTriMesh());

		this.modelToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<Backdrop>("backdrop/Backdrop");
		this.vertexShader = vertexShaderAndBytecode;
		this.inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, MeshBuffers.InputElements);

		this.pixelShader = shaderCache.GetPixelShader<Backdrop>("backdrop/Backdrop");
		
		this.transform = transform;
	}

	public void Dispose() {
		meshBuffers.Dispose();
		modelToWorldTransform.Dispose();
		inputLayout.Dispose();
	}
	
	public void Render(DeviceContext context) {
		modelToWorldTransform.Update(context, transform);

		context.InputAssembler.InputLayout = inputLayout;
		meshBuffers.Bind(context.InputAssembler);

		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(1, modelToWorldTransform.Buffer);

		context.PixelShader.Set(pixelShader);

		meshBuffers.Draw(context);
	}
}