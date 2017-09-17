using SharpDX.Direct3D11;
using System;

public class States : IDisposable {
	private readonly RasterizerState rasterizer;
	private readonly DepthStencilState depthStencil;
	private readonly BlendState blend;

	public States(Device device, StateDescriptions description) {
		rasterizer = new RasterizerState(device, description.rasterizer);
		depthStencil = new DepthStencilState(device, description.depthStencil);
		blend = new BlendState(device, description.blend);
	}

	public void Dispose() {
		rasterizer.Dispose();
		depthStencil.Dispose();
		blend.Dispose();
	}

	public void Apply(DeviceContext context) {
		context.Rasterizer.State = rasterizer;
		context.OutputMerger.SetDepthStencilState(depthStencil);
		context.OutputMerger.SetBlendState(blend);
	}
}
