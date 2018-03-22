using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Device = SharpDX.Direct3D11.Device;

public class OitBlendTarget : IDisposable {
	private const Format ColorFormat = Format.R16G16B16A16_Float;
	private const Format RevealageFormat = Format.R16_Float;
	private const Format DepthTextureFormat = Format.R24G8_Typeless;
	private const Format DepthTargetFormat = Format.D24_UNorm_S8_UInt;
	private const Format DepthResourceFormat = Format.R24_UNorm_X8_Typeless;

	public Texture2D AccumTexture { get; }
	private RenderTargetView AccumTargetView { get; }
	public ShaderResourceView AccumResourceView { get; }

	public Texture2D RevealageTexture { get; }
	private RenderTargetView RevealageTargetView { get; }
	public ShaderResourceView RevealageSourceView { get; }

	public Texture2D DepthTexture { get; }
	public DepthStencilView DepthTargetView { get; }
	public ShaderResourceView DepthSourceView { get; }
	
	private readonly VertexShader fullScreenVertexShader;
	private readonly States depthCopyingStates;
	private readonly PixelShader depthCopyingShader;

	private readonly States compositingStates;
	private readonly PixelShader compositingShader;

	public OitBlendTarget(Device device, ShaderCache shaderCache, Size2 size) {
		AccumTexture = new Texture2D(device, new Texture2DDescription {
			Format = ColorFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = new SampleDescription(1, 0),
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
		});
		AccumTargetView = new RenderTargetView(device, AccumTexture);
		AccumResourceView = new ShaderResourceView(device, AccumTexture);

		RevealageTexture = new Texture2D(device, new Texture2DDescription() {
			Format = RevealageFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = new SampleDescription(1, 0),
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
			CpuAccessFlags = CpuAccessFlags.None,
			OptionFlags = ResourceOptionFlags.None
		});
		RevealageTargetView = new RenderTargetView(device, RevealageTexture);
		RevealageSourceView = new ShaderResourceView(device, RevealageTexture);

		DepthTexture = new Texture2D(device, new Texture2DDescription {
			Format = DepthTextureFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = new SampleDescription(1, 0),
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource
		});
		DepthTargetView = new DepthStencilView(device, DepthTexture, new DepthStencilViewDescription {
			Format = DepthTargetFormat,
			Dimension = DepthStencilViewDimension.Texture2D
		});
		DepthSourceView = new ShaderResourceView(device, DepthTexture, new ShaderResourceViewDescription {
			Format = DepthResourceFormat,
			Dimension = ShaderResourceViewDimension.Texture2D,
			Texture2D = new ShaderResourceViewDescription.Texture2DResource {
				MipLevels = 1,
				MostDetailedMip = 0
			}
		});
		
		StateDescriptions depthCopyingStateDesc = StateDescriptions.Common.Clone();
		depthCopyingStateDesc.rasterizer.CullMode = CullMode.None;
		depthCopyingStates = new States(device, depthCopyingStateDesc);
		
		StateDescriptions compositingStateDesc = StateDescriptions.Common.Clone();
		compositingStateDesc.rasterizer.CullMode = CullMode.None;
		compositingStates = new States(device, compositingStateDesc);

		fullScreenVertexShader = shaderCache.GetVertexShader<RenderPassController>("game/rendering/FullScreenVertexShader");
		depthCopyingShader = shaderCache.GetPixelShader<RenderPassController>("game/rendering/DepthCopyingShader");
		compositingShader = shaderCache.GetPixelShader<RenderPassController>("game/rendering/UnorderedTransparencyCompositingShader");
	}

	public void Dispose() {
		AccumTexture.Dispose();
		AccumTargetView.Dispose();
		AccumResourceView.Dispose();
		
		RevealageTexture.Dispose();
		RevealageTargetView.Dispose();
		RevealageSourceView.Dispose();

		DepthTexture.Dispose();
		DepthTargetView.Dispose();
		DepthSourceView.Dispose();

		depthCopyingStates.Dispose();
		compositingStates.Dispose();
	}

	private void DrawFullscreenQuad(DeviceContext context) {
		context.VertexShader.Set(fullScreenVertexShader);
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		context.Draw(4, 0);
	}

	public void CopyStandardDepthToBlendDepth(DeviceContext context, ShaderResourceView standardDepth) {
		depthCopyingStates.Apply(context);
		context.OutputMerger.SetRenderTargets(DepthTargetView);
		context.PixelShader.Set(depthCopyingShader);
		context.PixelShader.SetShaderResource(0, standardDepth);
		DrawFullscreenQuad(context);
	}

	public void SetAsTarget(DeviceContext context) {
		context.OutputMerger.SetRenderTargets(DepthTargetView, AccumTargetView, RevealageTargetView);
	}

	public void Composite(DeviceContext context, ShaderResourceView multisampleColorSourceView, ShaderResourceView multisampleDepthSourceView) {
		compositingStates.Apply(context);
		context.PixelShader.Set(compositingShader);
		context.PixelShader.SetShaderResources(0, AccumResourceView, RevealageSourceView, DepthSourceView, multisampleColorSourceView, multisampleDepthSourceView);
		DrawFullscreenQuad(context);
	}

	public void Prepare(DeviceContext context, Color color, byte stencil, Action prepareMask) {
		context.ClearRenderTargetView(AccumTargetView, new Color4(0, 0, 0, 0));
		context.ClearRenderTargetView(RevealageTargetView, new Color4(1, 1, 1, 1));
		context.ClearDepthStencilView(DepthTargetView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, RenderingConstants.DepthClearValue, stencil);
		context.OutputMerger.SetTargets(DepthTargetView, AccumTargetView);
		prepareMask();
	}
}
