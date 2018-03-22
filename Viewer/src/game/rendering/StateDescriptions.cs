using SharpDX.Direct3D11;

public struct StateDescriptions {
	public RasterizerStateDescription rasterizer;
	public DepthStencilStateDescription depthStencil;
	public BlendStateDescription blend;

	public static StateDescriptions Default() {
		return new StateDescriptions {
			rasterizer = RasterizerStateDescription.Default(),
			depthStencil = DepthStencilStateDescription.Default(),
			blend = BlendStateDescription.Default()
		};
	}

	public static StateDescriptions Common {
		get {
			StateDescriptions desc = StateDescriptions.Default();
			
			//standard for DSON models
			desc.rasterizer.IsFrontCounterClockwise = true;

			//turn on stencil test for hidden area mesh
			desc.depthStencil.IsStencilEnabled = true;
			desc.depthStencil.FrontFace.Comparison = Comparison.NotEqual;
			desc.depthStencil.BackFace.Comparison = Comparison.NotEqual;

			desc.depthStencil.DepthComparison = Comparison.Greater;

			return desc;
		}
	}
		
	public StateDescriptions Clone() {
		return new StateDescriptions {
			rasterizer = this.rasterizer,
			depthStencil = this.depthStencil,
			blend = this.blend.Clone()
		};
	}
}
