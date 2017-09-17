using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System;

public class StandardSamplers : IDisposable {
	private readonly SamplerState[] samplers;
	
	public StandardSamplers(Device device) {
		SamplerStateDescription trilinearDesc = SamplerStateDescription.Default();
		trilinearDesc.Filter = Filter.MinMagMipLinear;
		SamplerState trilinear = new SamplerState(device, trilinearDesc);

		SamplerStateDescription anisotropicDesc = SamplerStateDescription.Default();
		anisotropicDesc.Filter = Filter.Anisotropic;
		anisotropicDesc.MaximumAnisotropy = 4;
		SamplerState anisotropic = new SamplerState(device, anisotropicDesc);
		
		SamplerStateDescription uiDesc = SamplerStateDescription.Default();
		uiDesc.AddressU = TextureAddressMode.Border;
		uiDesc.AddressV = TextureAddressMode.Border;
		uiDesc.AddressW = TextureAddressMode.Border;
		uiDesc.BorderColor = new RawColor4(0, 0, 0, 0);
		uiDesc.Filter = Filter.MinMagMipLinear;
		uiDesc.MipLodBias = -0.65f;
		SamplerState ui = new SamplerState(device, uiDesc);

		samplers = new SamplerState[] {trilinear, anisotropic, ui};
	}
	
	public void Dispose() {
		foreach (var sampler in samplers) {
			sampler.Dispose();
		}
	}

	public void Apply(CommonShaderStage stage) {
		stage.SetSamplers(0, samplers);
	}

}
