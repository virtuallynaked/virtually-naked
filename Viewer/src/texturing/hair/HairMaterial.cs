using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;

class HairMaterial : IMaterial {
	[StructLayout(LayoutKind.Explicit, Size = 4 * 4)]
	public struct Constants {
		[FieldOffset(0 * 4)] public Vector3 diffuseAlbedo;
		[FieldOffset(3 * 4)] public float opacity;
	}

	public struct Textures {
		public ShaderResourceView diffuseAlbedo;
		public ShaderResourceView opacity;
	}

	public static HairMaterial Load(Device device, ShaderCache shaderCache, TextureLoader textureLoader, HairMaterialSettings settings) {
		Constants constants = new Constants { };
		Textures textures = new Textures { };

		constants.diffuseAlbedo = settings.diffuseAlbedo.value;
		textures.diffuseAlbedo = textureLoader.Load(settings.diffuseAlbedo.image, TextureLoader.DefaultMode.Standard);

		constants.opacity = settings.opacity.value;
		textures.opacity = textureLoader.Load(settings.opacity.image, TextureLoader.DefaultMode.Standard);
		
		ShaderResourceView[] texturesViews = new [] {
			textures.diffuseAlbedo,
			textures.opacity
		};

		var constantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, ref constants, usage: ResourceUsage.Immutable);

		return new HairMaterial(device, shaderCache, settings, constantBuffer, texturesViews);
	}

	private readonly PixelShader standardShader;
	private readonly PixelShader unorderedTransparencyShader;
	private readonly HairMaterialSettings settings;
	private readonly Buffer constantBuffer;
	private readonly ShaderResourceView[] textureViews;

	public HairMaterial(Device device, ShaderCache shaderCache, HairMaterialSettings settings, Buffer constantBuffer, ShaderResourceView[] textureViews) {
		this.settings = settings;
		this.constantBuffer = constantBuffer;
		this.textureViews = textureViews;

		standardShader = shaderCache.GetPixelShader<UberMaterial>("texturing/hair/HairShader-Standard");
		unorderedTransparencyShader = shaderCache.GetPixelShader<UberMaterial>("texturing/hair/HairShader-UnorderedTransparency");
	}
	
	public void Dispose() {
		constantBuffer?.Dispose();
		//don't dispose texture view because it's owned by the texture loader
	}
	
	public string UvSet => settings.uvSet;

	private PixelShader PickShader(OutputMode mode) {
		switch (mode) {
			case OutputMode.Standard:
				return standardShader;
			case OutputMode.WeightedBlendedOrderIndependent:
				return unorderedTransparencyShader;
			default:
				return null;
		}
	}
	
	public void Apply(DeviceContext context, OutputMode outputMode) {
		context.PixelShader.Set(PickShader(outputMode));
		context.PixelShader.SetShaderResources(ShaderSlots.MaterialTextureStart, textureViews);
		context.PixelShader.SetConstantBuffer(ShaderSlots.MaterialConstantBufferStart, constantBuffer);
	}
	
	public void Unapply(DeviceContext context) {
		context.PixelShader.Set(null);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, null);
		context.PixelShader.SetConstantBuffer(ShaderSlots.MaterialConstantBufferStart, null);
	}
}
