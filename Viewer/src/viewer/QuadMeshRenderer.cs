using SharpDX;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

class QuadMeshRenderer {
	private readonly QuadMeshBuffers meshBuffers;
	private readonly CoordinateNormalMatrixPairConstantBufferManager modelToWorldTransform;
	private readonly VertexShader vertexShader;
	private readonly InputLayout inputLayout;
	private readonly IOpaqueMaterial material;

	private readonly Matrix transform;

	public QuadMeshRenderer(Device device, ShaderCache shaderCache, Matrix transform, QuadMesh mesh) {
		this.meshBuffers = new QuadMeshBuffers(device, mesh);

		this.modelToWorldTransform = new CoordinateNormalMatrixPairConstantBufferManager(device);

		var vertexShaderAndBytecode = shaderCache.GetVertexShader<QuadMeshRenderer>("viewer/QuadMeshVertex");
		this.vertexShader = vertexShaderAndBytecode;
		this.inputLayout = new InputLayout(device, vertexShaderAndBytecode.Bytecode, QuadMeshBuffers.InputElements);

		using (var texture = MonochromaticTextures.Make(device, Color.LightGray)) {
			var textureView = new ShaderResourceView(device, texture);
			this.material = new BasicSpecularMaterial(device, shaderCache, textureView);
		}

		this.transform = transform;
	}

	public void Dispose() {
		meshBuffers.Dispose();
		modelToWorldTransform.Dispose();
		inputLayout.Dispose();
		material.Dispose();
	}
	
	public void Render(DeviceContext context) {
		modelToWorldTransform.Update(context, transform);

		context.InputAssembler.InputLayout = inputLayout;
		meshBuffers.Bind(context.InputAssembler);

		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(1, modelToWorldTransform.Buffer);

		material.Apply(context);

		meshBuffers.Draw(context);
	}
}