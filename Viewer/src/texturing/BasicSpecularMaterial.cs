using SharpDX.Direct3D11;
using System.Runtime.InteropServices;

public class BasicSpecularMaterial : IOpaqueMaterial {
	public const string ShaderName = "texturing/BasicSpecularMaterial";

	public class Factory {
		private readonly Device device;
		private readonly ShaderCache shaderCache;

		public Factory(Device device, ShaderCache shaderCache) {
			this.device = device;
			this.shaderCache = shaderCache;
		}

		public IOpaqueMaterial Make(ShaderResourceView albedoTexture, float roughness = 0.5f, float specular = 0.04f) {
			return new BasicSpecularMaterial(device, shaderCache, albedoTexture, roughness, specular);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 16 * 1)]
	public struct Constants {
		[FieldOffset(0)] public float roughness;
		[FieldOffset(4)] public float specular;
	}

	private readonly PixelShader shader;
	private readonly ShaderResourceView albedoTexture;
	private readonly Buffer constantBuffer;

	public BasicSpecularMaterial(Device device, ShaderCache shaderCache, ShaderResourceView albedoTexture, float roughness = 0.5f, float specular = 0.04f) {
		this.shader = shaderCache.GetPixelShader<BasicSpecularMaterial>(ShaderName);

		this.albedoTexture = albedoTexture;

		Constants constants = new Constants {
			roughness = roughness,
			specular = specular
		};
		this.constantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, ref constants, usage: ResourceUsage.Immutable);
	}

	public void Dispose() {
		albedoTexture.Dispose();
		constantBuffer.Dispose();
	}

	public void Apply(DeviceContext context) {
		context.PixelShader.Set(shader);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, albedoTexture);
		context.PixelShader.SetConstantBuffer(ShaderSlots.MaterialConstantBufferStart, constantBuffer);
	}
}
