using SharpDX;
using SharpDX.Direct3D11;
using System;
using Device = SharpDX.Direct3D11.Device;

public class RenderPassController : IDisposable {
	private Viewport viewport;

	private StandardTarget standardTarget;
	private OitBlendTarget oitBlendTarget;
	private readonly PostProcessor postProcessor;
	
	private readonly States opaquePassStates;
	private readonly States backToFrontTransparencyPassStates;
	private readonly States unorderedTransparencyPassStates;
	
	private readonly IMenuLevel renderSettingsMenuLevel;

	public RenderPassController(Device device, ShaderCache shaderCache, Size2 targetSize) {
		this.viewport = new Viewport(
			0, 0,
			targetSize.Width, targetSize.Height,
			0.0f, 1.0f);

		standardTarget = new StandardTarget(device, targetSize);
		oitBlendTarget = new OitBlendTarget(device, shaderCache, targetSize);
		postProcessor = new PostProcessor(device, shaderCache, targetSize);
		
		renderSettingsMenuLevel = new ToneMappingMenuLevel(postProcessor.ToneMappingSettings);

		StateDescriptions opaquePassStateDesc = StateDescriptions.Common.Clone();
		opaquePassStateDesc.rasterizer.IsMultisampleEnabled = true;
		opaquePassStateDesc.rasterizer.CullMode = CullMode.Back;
		this.opaquePassStates = new States(device, opaquePassStateDesc);

		StateDescriptions backToFrontTransparencyPassStateDesc = StateDescriptions.Common.Clone();
		backToFrontTransparencyPassStateDesc.rasterizer.IsMultisampleEnabled = true;
		backToFrontTransparencyPassStateDesc.rasterizer.CullMode = CullMode.None;
		backToFrontTransparencyPassStateDesc.depthStencil.DepthWriteMask = DepthWriteMask.Zero;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].IsBlendEnabled = true;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].SourceBlend = BlendOption.One;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
		this.backToFrontTransparencyPassStates = new States(device, backToFrontTransparencyPassStateDesc);
		
		StateDescriptions unorderedTransparencyStateDesc = StateDescriptions.Common.Clone();
		unorderedTransparencyStateDesc.rasterizer.IsMultisampleEnabled = false;
		unorderedTransparencyStateDesc.rasterizer.CullMode = CullMode.None;
		unorderedTransparencyStateDesc.depthStencil.DepthWriteMask = DepthWriteMask.Zero;
		unorderedTransparencyStateDesc.blend.IndependentBlendEnable = true;
		unorderedTransparencyStateDesc.blend.RenderTarget[0].IsBlendEnabled = true;
		unorderedTransparencyStateDesc.blend.RenderTarget[0].SourceBlend = BlendOption.One;
		unorderedTransparencyStateDesc.blend.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
		unorderedTransparencyStateDesc.blend.RenderTarget[0].DestinationBlend = BlendOption.One;
		unorderedTransparencyStateDesc.blend.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
		unorderedTransparencyStateDesc.blend.RenderTarget[1].IsBlendEnabled = true;
		unorderedTransparencyStateDesc.blend.RenderTarget[1].SourceBlend = BlendOption.Zero;
		unorderedTransparencyStateDesc.blend.RenderTarget[1].SourceAlphaBlend = BlendOption.Zero;
		unorderedTransparencyStateDesc.blend.RenderTarget[1].DestinationBlend = BlendOption.InverseSourceAlpha;
		unorderedTransparencyStateDesc.blend.RenderTarget[1].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
		this.unorderedTransparencyPassStates = new States(device, unorderedTransparencyStateDesc);
	}
	
	public void Dispose() {
		standardTarget.Dispose();
		oitBlendTarget.Dispose();
		postProcessor.Dispose();

		opaquePassStates.Dispose();
		backToFrontTransparencyPassStates.Dispose();
		unorderedTransparencyPassStates.Dispose();
	}

	public Texture2D ResultTexture => postProcessor.ResultTexture;
	public ShaderResourceView ResultSourceView => postProcessor.ResultSourceView;
	public IMenuLevel RenderSettingsMenuLevel => renderSettingsMenuLevel;

	public void PrepareFrame(DeviceContext context) {
		postProcessor.Prepare(context);
	}

	public void Prepare(DeviceContext context, Color color, byte stencil, Action prepareMask) {
		context.Rasterizer.SetViewport(viewport);

		standardTarget.Prepare(context, color, stencil, prepareMask);
		oitBlendTarget.Prepare(context, color, stencil, prepareMask);
	}
	
	public void RenderAllPases(DeviceContext context, Action<RenderingPass> render) {
		context.Rasterizer.SetViewport(viewport);

		//Switch to standard target
		standardTarget.SetAsTarget(context);
		
		//Opaque Pass
		opaquePassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.Opaque, OutputMode.Standard));

		//Back-to-Front-Transparency Pass
		backToFrontTransparencyPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.BackToFrontTransparent, OutputMode.Standard));

		//Switch to oit-blend target
		oitBlendTarget.CopyStandardDepthToBlendDepth(context, standardTarget.DepthResourceView);
		oitBlendTarget.SetAsTarget(context);

		//Unordered Transparency Blend Pass
		unorderedTransparencyPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.UnorderedTransparent, OutputMode.WeightedBlendedOrderIndependent));

		//Resolve and composite
		standardTarget.SetResolveAsTarget(context);
		oitBlendTarget.Composite(context, standardTarget.RenderSourceView, standardTarget.DepthResourceView);
		
		postProcessor.PostProcess(context, standardTarget.ResolveSourceView);

		//Render UI
		backToFrontTransparencyPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.UiElements, OutputMode.Standard));
	}
}
