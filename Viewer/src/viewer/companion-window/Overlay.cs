using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

public class Overlay : IDisposable {

	public static Overlay Load(Device device, ShaderCache shaderCache, IArchiveFile textureFile) {
		Size2 overlaySize;
		ShaderResourceView overlayResourceView;
		using (var dataView = textureFile.OpenDataView()) {
			DdsLoader.CreateDDSTextureFromMemory(device, dataView.DataPointer, out var texture, out overlayResourceView);
			var desc = (texture as Texture2D).Description;
			overlaySize = new Size2(desc.Width, desc.Height);
			texture.Dispose();
		}

		return new Overlay(device, shaderCache, overlaySize, overlayResourceView);
	}
	
	private readonly Size2 overlaySize;
	private readonly ShaderResourceView overlayTextureView;
	private readonly VertexShader fullScreenVertexShader;
	private readonly PixelShader pixelShader;

	public Overlay(Device device, ShaderCache shaderCache, Size2 overlaySize, ShaderResourceView overlayTextureView) {
		this.overlaySize = overlaySize;
		this.overlayTextureView = overlayTextureView;

		fullScreenVertexShader = shaderCache.GetVertexShader<RenderPassController>("game/rendering/FullScreenVertexShader");
		pixelShader = shaderCache.GetPixelShader<Overlay>("viewer/companion-window/Overlay");
	}
	
	public void Dispose() {
		overlayTextureView.Dispose();
	}

	private void DrawFullscreenQuad(DeviceContext context) {
		context.VertexShader.Set(fullScreenVertexShader);
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		context.Draw(4, 0);
	}

	public void Draw(DeviceContext context) {
		context.Rasterizer.SetViewport(0, 0, overlaySize.Width, overlaySize.Height);

		context.PixelShader.Set(pixelShader);
		context.PixelShader.SetShaderResource(0, overlayTextureView);

		DrawFullscreenQuad(context);
	}
}
