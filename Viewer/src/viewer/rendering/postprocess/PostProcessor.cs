using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

public class PostProcessor : IDisposable {
	[StructLayout(LayoutKind.Explicit, Size = 8 * 4)]
	private struct PostProcessorConstants {
		[FieldOffset(0 * 0)] public Vector3 exposureAndBalanceAdjustment;
		[FieldOffset(3 * 4)] public float burnHighlightsValue;
		[FieldOffset(4 * 4)] public float crushBlacksValue;
	};

	private const Format ColorFormat = Format.B8G8R8X8_UNorm_SRgb;

	public Texture2D ResultTexture { get; }
	private RenderTargetView ResultTargetView { get; }
	public ShaderResourceView ResultSourceView { get; }
	
	private ConstantBufferManager<PostProcessorConstants> constantsBuffer;

	private readonly States postProcessingStates;
	
	private readonly VertexShader fullScreenVertexShader;
	private readonly PixelShader postProcessingShader;

	public PostProcessor(Device device, ShaderCache shaderCache, Size2 size) {
		ResultTexture = new Texture2D(device, new Texture2DDescription() {
			Format = ColorFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = new SampleDescription(1, 0),
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
		});
		ResultTargetView = new RenderTargetView(device, ResultTexture);
		ResultSourceView = new ShaderResourceView(device, ResultTexture);

		constantsBuffer = new ConstantBufferManager<PostProcessorConstants>(device);

		StateDescriptions postProcessingStateDesc = StateDescriptions.Common.Clone();
		postProcessingStateDesc.rasterizer.CullMode = CullMode.None;
		postProcessingStates = new States(device, postProcessingStateDesc);

		fullScreenVertexShader = shaderCache.GetVertexShader<RenderPassController>("viewer/rendering/FullScreenVertexShader");
		postProcessingShader = shaderCache.GetPixelShader<RenderPassController>("viewer/rendering/postprocess/PostProcessingShader");
	}

	public void Dispose() {
		ResultTexture.Dispose();
		ResultTargetView.Dispose();
		ResultSourceView.Dispose();
		constantsBuffer.Dispose();
		postProcessingStates.Dispose();
	}

	private void DrawFullscreenQuad(DeviceContext context) {
		context.VertexShader.Set(fullScreenVertexShader);
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		context.Draw(4, 0);
	}

	private static readonly Vector3 NeutralWhiteBalanceAsLinearSRGB = ColorConversion.FromTemperatureToLinearSRGB(ToneMappingSettings.NeutralWhiteBalance);
	private PostProcessorConstants GenerateConstants(ToneMappingSettings toneMappingSettings) {
		float exposureAdjustment = (float) Math.Pow(2, toneMappingSettings.ExposureValue - 12);
		Vector3 balanceAdjustment = ColorConversion.FromTemperatureToLinearSRGB(toneMappingSettings.WhiteBalance) / NeutralWhiteBalanceAsLinearSRGB;

		return new PostProcessorConstants {
			exposureAndBalanceAdjustment = exposureAdjustment * balanceAdjustment,
			burnHighlightsValue = (float) toneMappingSettings.BurnHighlights,
			crushBlacksValue = (float) toneMappingSettings.CrushBlacks
		};
	}

	public void Prepare(DeviceContext context, ToneMappingSettings toneMappingSettings) {
		constantsBuffer.Update(context, GenerateConstants(toneMappingSettings));
	}

	public void PostProcess(DeviceContext context, ShaderResourceView source) {
		postProcessingStates.Apply(context);
		context.OutputMerger.SetTargets(ResultTargetView);
		context.PixelShader.Set(postProcessingShader);
		context.PixelShader.SetShaderResource(0, source);
		context.PixelShader.SetConstantBuffer(0, constantsBuffer.Buffer);
		DrawFullscreenQuad(context);
	}

}
