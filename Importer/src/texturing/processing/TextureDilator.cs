using System;
using SharpDX;
using SharpDX.Direct3D11;

public class TextureDilator : IDisposable {
	private const int ShaderNumThreadsPerDim = 16;

	private readonly Device device;

	private readonly TextureMaskRenderer maskRenderer;
	private readonly ComputeShader alphaPremultiplerShader;
	private readonly ComputeShader dilatorShader;
	
	public TextureDilator(Device device, ShaderCache shaderCache) {
		this.device = device;
		
		maskRenderer = new TextureMaskRenderer(device, shaderCache);
		alphaPremultiplerShader = shaderCache.GetComputeShader<TextureDilator>("texturing/processing/AlphaPremultiplier");
		dilatorShader = shaderCache.GetComputeShader<TextureDilator>("texturing/processing/Dilator");
	}
	
	public void Dispose() {
		maskRenderer.Dispose();
	}
	
	public void Dilate(TextureMask mask, Size2 size, bool isLinear, DataBox imageData) {
		//set every alpha value to 0
		for (int i = 0; i < imageData.SlicePitch; i += 4) {
			var rgba = Utilities.Read<uint>(imageData.DataPointer + i);
			rgba &= 0xffffff;
			Utilities.Write<uint>(imageData.DataPointer + i, ref rgba);
		}

		var sourceTextureDesc = new Texture2DDescription {
			Width = size.Width,
			Height = size.Height,
			MipLevels = 0,
			ArraySize = 1,
			Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
			SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
			BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
			OptionFlags = ResourceOptionFlags.GenerateMipMaps
		};
		var sourceTexture = new Texture2D(device, sourceTextureDesc);
		var sourceTextureInView = new ShaderResourceView(device, sourceTexture);
		var sourceTextureOutView = new UnorderedAccessView(device, sourceTexture);
		
		var destTextureDesc = new Texture2DDescription {
			Width = size.Width,
			Height = size.Height,
			MipLevels = 1,
			ArraySize = 1,
			Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
			SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
			BindFlags = BindFlags.UnorderedAccess
		};
		var destTexture = new Texture2D(device, destTextureDesc);
		var destTextureOutView = new UnorderedAccessView(device, destTexture);

		var stagingTextureDesc = new Texture2DDescription {
			Width = size.Width,
			Height = size.Height,
			MipLevels = 1,
			ArraySize = 1,
			Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
			SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
			Usage = ResourceUsage.Staging,
			CpuAccessFlags = CpuAccessFlags.Read
		};
		var stagingTexture = new Texture2D(device, stagingTextureDesc);
		
		var context = device.ImmediateContext;

		context.UpdateSubresource(imageData, sourceTexture, 0);

		maskRenderer.RenderMaskToAlpha(mask, size, sourceTexture);
		
		context.ClearState();
		context.ComputeShader.Set(alphaPremultiplerShader);
		context.ComputeShader.SetUnorderedAccessView(0, sourceTextureOutView);
		context.Dispatch(
			IntegerUtils.RoundUp(size.Width, ShaderNumThreadsPerDim),
			IntegerUtils.RoundUp(size.Height, ShaderNumThreadsPerDim),
			1);
		context.ClearState();

		context.GenerateMips(sourceTextureInView);
		
		context.ClearState();
		context.ComputeShader.Set(dilatorShader);
		context.ComputeShader.SetShaderResources(0, sourceTextureInView);
		context.ComputeShader.SetUnorderedAccessView(0, destTextureOutView);
		context.Dispatch(
			IntegerUtils.RoundUp(size.Width, ShaderNumThreadsPerDim),
			IntegerUtils.RoundUp(size.Height, ShaderNumThreadsPerDim),
			1);
		context.ClearState();
		
		context.CopyResource(destTexture, stagingTexture);

		var resultImageData = context.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None);
		CopyDataBox(resultImageData, imageData);
		context.UnmapSubresource(stagingTexture, 0);

		stagingTexture.Dispose();

		destTexture.Dispose();
		destTextureOutView.Dispose();
		
		sourceTexture.Dispose();
		sourceTextureInView.Dispose();
		sourceTextureOutView.Dispose();
	}

	private static void CopyDataBox(DataBox source, DataBox dest) {
		var minPitch = Math.Min(source.RowPitch, dest.RowPitch);
		for (int sourceOffset = 0, destOffset = 0; sourceOffset < source.SlicePitch; sourceOffset += source.RowPitch, destOffset += dest.RowPitch) {
			Utilities.CopyMemory(dest.DataPointer + destOffset, source.DataPointer + sourceOffset, minPitch);
		}
	}
}
