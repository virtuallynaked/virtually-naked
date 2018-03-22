using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Device = SharpDX.Direct3D11.Device;

public class StandardTarget : IDisposable {
	private const int MsaaSampleCount = 4;

	private const Format ColorFormat = Format.R16G16B16A16_Float;
	private const Format DepthTextureFormat = Format.R24G8_Typeless;
	private const Format DepthTargetFormat = Format.D24_UNorm_S8_UInt;
	private const Format DepthResourceFormat = Format.R24_UNorm_X8_Typeless;

	public Texture2D RenderTexture { get; }
	private RenderTargetView RenderTargetView { get; }
	public ShaderResourceView RenderSourceView { get; }

	public Texture2D DepthTexture { get; }
	private DepthStencilView DepthTargetView { get; }
	public ShaderResourceView DepthResourceView { get; }

	public Texture2D ResolveTexture { get; }
	public RenderTargetView ResolveTargetView { get; }
	public ShaderResourceView ResolveSourceView { get; }
	
	public StandardTarget(Device device, Size2 size) {
		var sampleDescription = new SampleDescription(
			MsaaSampleCount,
			(int) StandardMultisampleQualityLevels.StandardMultisamplePattern);
		
		RenderTexture = new Texture2D(device, new Texture2DDescription {
			Format = ColorFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = sampleDescription,
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
		});
		RenderSourceView = new ShaderResourceView(device, RenderTexture);
		RenderTargetView = new RenderTargetView(device, RenderTexture);

		DepthTexture = new Texture2D(device, new Texture2DDescription {
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			Format = DepthTextureFormat,
			SampleDescription = sampleDescription,
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource
		});

		DepthTargetView = new DepthStencilView(device, DepthTexture, new DepthStencilViewDescription {
			Format = DepthTargetFormat,
			Dimension = DepthStencilViewDimension.Texture2DMultisampled
		});
		
		DepthResourceView = new ShaderResourceView(device, DepthTexture, new ShaderResourceViewDescription {
			Format = DepthResourceFormat,
			Dimension = ShaderResourceViewDimension.Texture2DMultisampled,
			Texture2D = new ShaderResourceViewDescription.Texture2DResource {
				MipLevels = 1,
				MostDetailedMip = 0
			}
		});

		ResolveTexture = new Texture2D(device, new Texture2DDescription() {
			Format = ColorFormat,
			Width = size.Width,
			Height = size.Height,
			ArraySize = 1,
			SampleDescription = new SampleDescription(1, 0),
			MipLevels = 1,
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
		});
		ResolveTargetView = new RenderTargetView(device, ResolveTexture);
		ResolveSourceView = new ShaderResourceView(device, ResolveTexture);
	}

	public void Dispose() {
		RenderTexture.Dispose();
		DepthTexture.Dispose();
		RenderTargetView.Dispose();
		RenderSourceView.Dispose();
		DepthTargetView.Dispose();
		DepthResourceView.Dispose();
		ResolveTexture.Dispose();
		ResolveTargetView.Dispose();
		ResolveSourceView.Dispose();
	}
	
	public void SetAsTarget(DeviceContext context) {
		context.OutputMerger.SetRenderTargets(DepthTargetView, RenderTargetView);
	}

	public void SetResolveAsTarget(DeviceContext context) {
		context.OutputMerger.SetTargets(ResolveTargetView);
	}

	public void Prepare(DeviceContext context, Color color, byte stencil, Action prepareMask) {
		context.ClearRenderTargetView(RenderTargetView, color);
		context.ClearDepthStencilView(DepthTargetView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, RenderingConstants.DepthClearValue, stencil);
		SetAsTarget(context);
		prepareMask();
	}
	
	public void Resolve(DeviceContext context) {
		context.ResolveSubresource(RenderTexture, 0, ResolveTexture, 0, ColorFormat);
	}
}
