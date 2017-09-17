using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Buffer = SharpDX.Direct3D11.Buffer;

[StructLayout(LayoutKind.Explicit, Size = 192)]
public struct UberConstants {
	//Base / Mixing
	[FieldOffset(0)] public int baseMixingMode;

	//Base / Diffuse / Reflection
	[FieldOffset(4)] public float metallicWeight;
	[FieldOffset(8)] public float diffuseWeight;
	[FieldOffset(16)] public Vector3 baseColor;
	
	//Base / Diffuse Translucency
	[FieldOffset(28)] public float translucencyWeight;
	[FieldOffset(32)] public int baseColorEffect;
	[FieldOffset(36)] public Vector3 translucencyColor;
	[FieldOffset(48)] public Vector3 sssReflectanceTint;

	//Base / Glossy / Reflection
	[FieldOffset(60)] public float glossyWeight;
	[FieldOffset(64)] public float glossyLayeredWeight;
	[FieldOffset(68)] public Vector3 glossyColor;
	[FieldOffset(80)] public int glossyColorEffect;
	[FieldOffset(84)] public Vector3 glossySpecular;
	[FieldOffset(96)] public float glossiness;
	[FieldOffset(100)] public float glossyReflectivity;
	[FieldOffset(104)] public float glossyRoughness;

	//Base / Glossy / Refraction
	[FieldOffset(108)] public float refractionIndex;
	[FieldOffset(112)] public float refractionWeight;

	//Base / Bump
	[FieldOffset(116)] public float bumpStrength;
	[FieldOffset(120)] public float normalMap;

	// Top Coat
	[FieldOffset(124)] public float topCoatWeight;
	[FieldOffset(128)] public Vector3 topCoatColor;
	[FieldOffset(140)] public int topCoatColorEffect;
	[FieldOffset(144)] public float topCoatRoughness;
	[FieldOffset(148)] public int topCoatLayeringMode;
	[FieldOffset(152)] public float topCoatReflectivity;
	[FieldOffset(156)] public float topCoatIOR;
	[FieldOffset(160)] public float topCoatCurveNormal;
	[FieldOffset(164)] public float topCoatCurveGrazing;

	// Top Coat / Bump
	[FieldOffset(168)] public float topCoatBump;

	// Volume
	[FieldOffset(172)] public int thinWalled;
	[FieldOffset(176)] public Vector3 volumeColor;

	// Geometry/Cutout
	[FieldOffset(188)] public float cutoutOpacity;
}

public struct UberTextures {
	//Base / Diffuse / Reflection
	public ShaderResourceView metallicWeight;
	public ShaderResourceView diffuseWeight;
	public ShaderResourceView baseColor;

	//Base / Diffuse Translucency
	public ShaderResourceView translucencyWeight;
	public ShaderResourceView translucencyColor;

	//Base / Glossy / Reflection
	public ShaderResourceView glossyWeight;
	public ShaderResourceView glossyLayeredWeight;
	public ShaderResourceView glossyColor;
	public ShaderResourceView glossySpecular;
	public ShaderResourceView glossiness;
	public ShaderResourceView glossyReflectivity;
	public ShaderResourceView glossyRoughness;

	//Base / Glossy / Refraction
	public ShaderResourceView refractionWeight;

	//Base / Bump
	public ShaderResourceView bumpStrength;
	public ShaderResourceView normalMap;

	// Top Coat
	public ShaderResourceView topCoatWeight;
	public ShaderResourceView topCoatColor;
	public ShaderResourceView topCoatRoughness;
	public ShaderResourceView topCoatReflectivity;
	public ShaderResourceView topCoatIOR;
	public ShaderResourceView topCoatCurveNormal;
	public ShaderResourceView topCoatCurveGrazing;

	//Top Coat Bump
	public ShaderResourceView topCoatBump;

	//Geometry/Cutout
	public ShaderResourceView cutoutOpacity;

	public ShaderResourceView[] ToArray() {
		return new ShaderResourceView[] {
			metallicWeight, diffuseWeight, baseColor,
			translucencyWeight, translucencyColor,
			glossyWeight, glossyLayeredWeight, glossyColor, glossySpecular,
			glossiness, glossyReflectivity, glossyRoughness,
			refractionWeight,
			bumpStrength, normalMap,
			topCoatWeight, topCoatColor, topCoatRoughness, topCoatReflectivity,
			topCoatIOR, topCoatCurveNormal, topCoatCurveGrazing,
			topCoatBump,
			cutoutOpacity
		};
	}
}

public class UberMaterial : IMaterial {
	public const string StandardShaderName = "texturing/uber/UberShader";
	public const string UnorderedTransparencyShaderName = "texturing/uber/UberShader-UnorderedTransparency";

	private readonly PixelShader standardShader;
	private readonly PixelShader unorderedTransparencyShader;
	private readonly UberMaterialSettings settings;
	private readonly Buffer constantBuffer;
	private readonly ShaderResourceView[] textureViews;
	
	private static void SetColorTexture(TextureLoader textureLoader, ColorTexture colorTexture, out Vector3 value, out ShaderResourceView textureView) {
		value = colorTexture.value;
		textureView = textureLoader.Load(colorTexture.image, TextureLoader.DefaultMode.Standard);
	}

	private static void SetFloatTexture(TextureLoader textureLoader, FloatTexture colorTexture, out float value, out ShaderResourceView textureView) {
		value = colorTexture.value;
		textureView = textureLoader.Load(colorTexture.image, TextureLoader.DefaultMode.Standard);
	}

	private static void SetBumpTexture(TextureLoader textureLoader, FloatTexture colorTexture, out float value, out ShaderResourceView textureView) {
		value = colorTexture.value;
		textureView = textureLoader.Load(colorTexture.image, TextureLoader.DefaultMode.Bump);
	}

	public static UberMaterial Load(Device device, ShaderCache shaderCache, TextureLoader textureLoader, UberMaterialSettings settings) {
		UberConstants constants;
		UberTextures textures;

		//Base / Mixing
		constants.baseMixingMode = settings.baseMixing;

		//Base / Diffuse / Reflection
		SetFloatTexture(textureLoader, settings.metallicWeight, out constants.metallicWeight, out textures.metallicWeight);
		SetFloatTexture(textureLoader, settings.diffuseWeight, out constants.diffuseWeight, out textures.diffuseWeight);
		SetColorTexture(textureLoader, settings.baseColor, out constants.baseColor, out textures.baseColor);
				
		//Base / Diffuse Translucency
		SetFloatTexture(textureLoader, settings.translucencyWeight, out constants.translucencyWeight, out textures.translucencyWeight);
		constants.baseColorEffect = settings.baseColorEffect;
		SetColorTexture(textureLoader, settings.translucencyColor, out constants.translucencyColor, out textures.translucencyColor);
		constants.sssReflectanceTint = settings.sssReflectanceTint;

		//Base / Glossy / Reflection
		SetFloatTexture(textureLoader, settings.glossyWeight, out constants.glossyWeight, out textures.glossyWeight);
		SetFloatTexture(textureLoader, settings.glossyLayeredWeight, out constants.glossyLayeredWeight, out textures.glossyLayeredWeight);
		SetColorTexture(textureLoader, settings.glossyColor, out constants.glossyColor, out textures.glossyColor);
		constants.glossyColorEffect = settings.glossyColorEffect;
		SetColorTexture(textureLoader, settings.glossySpecular, out constants.glossySpecular, out textures.glossySpecular);
		SetFloatTexture(textureLoader, settings.glossiness, out constants.glossiness, out textures.glossiness);
		SetFloatTexture(textureLoader, settings.glossyReflectivity, out constants.glossyReflectivity, out textures.glossyReflectivity);
		SetFloatTexture(textureLoader, settings.glossyRoughness, out constants.glossyRoughness, out textures.glossyRoughness);
		
		//Base / Glossy / Refraction
		constants.refractionIndex = settings.refractionIndex;
		SetFloatTexture(textureLoader, settings.refractionWeight, out constants.refractionWeight, out textures.refractionWeight);

		//Base Bump
		SetBumpTexture(textureLoader, settings.bumpStrength, out constants.bumpStrength, out textures.bumpStrength);
		SetBumpTexture(textureLoader, settings.normalMap, out constants.normalMap, out textures.normalMap);

		// Top Coat
		SetFloatTexture(textureLoader, settings.topCoatWeight, out constants.topCoatWeight, out textures.topCoatWeight);
		SetColorTexture(textureLoader, settings.topCoatColor, out constants.topCoatColor, out textures.topCoatColor);
		constants.topCoatColorEffect = settings.topCoatColorEffect;
		SetFloatTexture(textureLoader, settings.topCoatRoughness, out constants.topCoatRoughness, out textures.topCoatRoughness);
		constants.topCoatLayeringMode = settings.topCoatLayeringMode;
		SetFloatTexture(textureLoader, settings.topCoatReflectivity, out constants.topCoatReflectivity, out textures.topCoatReflectivity);
		SetFloatTexture(textureLoader, settings.topCoatIor, out constants.topCoatIOR, out textures.topCoatIOR);
		SetFloatTexture(textureLoader, settings.topCoatCurveNormal, out constants.topCoatCurveNormal, out textures.topCoatCurveNormal);
		SetFloatTexture(textureLoader, settings.topCoatCurveGrazing, out constants.topCoatCurveGrazing, out textures.topCoatCurveGrazing);
		
		// Top Coat Bump
		SetBumpTexture(textureLoader, settings.topCoatBump, out constants.topCoatBump, out textures.topCoatBump);

		// Volume
		constants.thinWalled = settings.thinWalled ? 1 : 0;
		var volumeColor = Vector3.Zero;
		for (int colorComponent = 0; colorComponent < 3; ++colorComponent) {
			var volumeParameters = new VolumeParameters(
				settings.transmittedMeasurementDistance,
				settings.transmittedColor[colorComponent],
				settings.scatteringMeasurementDistance,
				settings.sssAmount,
				settings.sssDirection);
			volumeColor[colorComponent] = (float) volumeParameters.SurfaceAlbedo;
		}
		constants.volumeColor = volumeColor;
		
		// Geometry/Cutout
		SetFloatTexture(textureLoader, settings.cutoutOpacity, out constants.cutoutOpacity, out textures.cutoutOpacity);
		
		var constantBuffer = Buffer.Create(device, BindFlags.ConstantBuffer, ref constants, usage: ResourceUsage.Immutable);
		
		return new UberMaterial(device, shaderCache, settings, constantBuffer, textures.ToArray());
	}
	
	public UberMaterial(Device device, ShaderCache shaderCache, UberMaterialSettings settings, Buffer constantBuffer, ShaderResourceView[] textureViews) {
		this.standardShader = shaderCache.GetPixelShader<UberMaterial>(StandardShaderName);
		this.unorderedTransparencyShader = shaderCache.GetPixelShader<UberMaterial>(UnorderedTransparencyShaderName);
		this.settings = settings;
		this.constantBuffer = constantBuffer;
		this.textureViews = textureViews;
	}

	public void Dispose() {
		constantBuffer.Dispose();
		//don't dispose texture view because it's owned by the texture loader
	}

	public UberMaterialSettings Settings => settings;

	public bool IsTransparent {
		get {
			if (settings.cutoutOpacity.value != 1 || settings.cutoutOpacity.image != null) {
				return true;
			}
			if (settings.thinWalled && settings.refractionWeight.value > 0) {
				return true;
			}
			return false;
		}
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
	
	public void Apply(DeviceContext context, RenderingPass pass) {
		context.PixelShader.Set(PickShader(pass.OutputMode));
		context.PixelShader.SetShaderResources(ShaderSlots.MaterialTextureStart, textureViews);
		context.PixelShader.SetConstantBuffer(ShaderSlots.MaterialConstantBufferStart, constantBuffer);
	}

	public void Unapply(DeviceContext context) {
		context.PixelShader.Set(null);
		context.PixelShader.SetShaderResource(ShaderSlots.MaterialTextureStart, null);
		context.PixelShader.SetConstantBuffer(ShaderSlots.MaterialConstantBufferStart, null);
	}
}
