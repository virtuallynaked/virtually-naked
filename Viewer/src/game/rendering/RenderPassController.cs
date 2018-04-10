using SharpDX;
using SharpDX.Direct3D11;
using System;
using Device = SharpDX.Direct3D11.Device;

public class RenderPassController : IDisposable {
	private Viewport viewport;

	private StandardTarget standardTarget;
	private OitBlendTarget oitBlendTarget;
	private readonly PostProcessor postProcessor;
	
	private readonly States oneSidedOpaquePassStates;
	private readonly States twoSidedOpaquePassStates;
	private readonly States backToFrontTransparencyBackFacesPassStates;
	private readonly States backToFrontTransparencyFrontFacesPassStates;
	private readonly States backToFrontTransparencyAllFacesPassStates;
	private readonly States unorderedTransparencyPassStates;

	public RenderPassController(Device device, ShaderCache shaderCache, Size2 targetSize) {
		this.viewport = new Viewport(
			0, 0,
			targetSize.Width, targetSize.Height,
			0.0f, 1.0f);

		standardTarget = new StandardTarget(device, targetSize);
		oitBlendTarget = new OitBlendTarget(device, shaderCache, targetSize);
		postProcessor = new PostProcessor(device, shaderCache, targetSize);

		StateDescriptions opaquePassStateDesc = StateDescriptions.Common.Clone();
		opaquePassStateDesc.rasterizer.IsMultisampleEnabled = true;
		opaquePassStateDesc.rasterizer.CullMode = CullMode.Back;
		this.oneSidedOpaquePassStates = new States(device, opaquePassStateDesc);
		opaquePassStateDesc.rasterizer.CullMode = CullMode.None;
		this.twoSidedOpaquePassStates = new States(device, opaquePassStateDesc);

		StateDescriptions backToFrontTransparencyPassStateDesc = StateDescriptions.Common.Clone();
		backToFrontTransparencyPassStateDesc.rasterizer.IsMultisampleEnabled = true;
		backToFrontTransparencyPassStateDesc.depthStencil.DepthWriteMask = DepthWriteMask.Zero;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].IsBlendEnabled = true;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].SourceBlend = BlendOption.One;
		backToFrontTransparencyPassStateDesc.blend.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
		backToFrontTransparencyPassStateDesc.rasterizer.CullMode = CullMode.Front;
		this.backToFrontTransparencyBackFacesPassStates = new States(device, backToFrontTransparencyPassStateDesc);
		backToFrontTransparencyPassStateDesc.rasterizer.CullMode = CullMode.Back;
		this.backToFrontTransparencyFrontFacesPassStates = new States(device, backToFrontTransparencyPassStateDesc);
		backToFrontTransparencyPassStateDesc.rasterizer.CullMode = CullMode.None;
		this.backToFrontTransparencyAllFacesPassStates = new States(device, backToFrontTransparencyPassStateDesc);
		
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

		oneSidedOpaquePassStates.Dispose();
		twoSidedOpaquePassStates.Dispose();
		backToFrontTransparencyBackFacesPassStates.Dispose();
		backToFrontTransparencyFrontFacesPassStates.Dispose();
		backToFrontTransparencyAllFacesPassStates.Dispose();
		unorderedTransparencyPassStates.Dispose();
	}

	public Texture2D ResultTexture => postProcessor.ResultTexture;
	public ShaderResourceView ResultSourceView => postProcessor.ResultSourceView;

	public void PrepareFrame(DeviceContext context, ToneMappingSettings toneMappingSettings) {
		postProcessor.Prepare(context, toneMappingSettings);
	}

	public void Prepare(DeviceContext context, Color color, byte stencil, Action prepareMask) {
		context.Rasterizer.SetViewport(viewport);

		standardTarget.Prepare(context, color, stencil, prepareMask);
		oitBlendTarget.Prepare(context, color, stencil, prepareMask);
	}
	
	public void RenderAllPases(DeviceContext context, Action<RenderingPass> render) {
		context.Rasterizer.SetViewport(viewport);

		//One-sided Opaque pass
		standardTarget.SetAsTarget(context);
		oneSidedOpaquePassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.OneSidedOpaque, OutputMode.Standard));

		//One-sided Back-to-Front-Transparency Pass
		backToFrontTransparencyAllFacesPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.OneSidedBackToFrontTransparent, OutputMode.Standard));

		//One-sided False-Depth Pass
		standardTarget.ClearDepth(context);
		standardTarget.SetAsDepthOnlyTarget(context);
		oneSidedOpaquePassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.OneSidedOpaque, OutputMode.FalseDepth));

		//Two-sided Opaque Pass
		standardTarget.SetAsTarget(context);
		twoSidedOpaquePassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.TwoSidedOpaque, OutputMode.Standard));

		//Two-sided Back-to-Front-Transparency Pass
		backToFrontTransparencyBackFacesPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.TwoSidedBackToFrontTransparent, OutputMode.Standard));
		backToFrontTransparencyFrontFacesPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.TwoSidedBackToFrontTransparent, OutputMode.Standard));

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
		backToFrontTransparencyAllFacesPassStates.Apply(context);
		render(new RenderingPass(RenderingLayer.UiElements, OutputMode.Standard));
	}
}
