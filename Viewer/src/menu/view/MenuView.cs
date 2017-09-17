using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

/** 
 * Menu view reads from the menu model and visualizes its state to a texture.
 * 
 * Rendering is done on a background thread to ensure it doesn't impact framerate. To avoid data-races, message passing is used:
 *	1) The MenuViewMessageAuthor authors a message describing the current view state.
 *	2) This message is passed to the rendering thread.
 *	3) The MenuViewMessageInterpreter interprets the message into WPF UI elements.
 *	4) These elements are rendered to a texture.
 */
public class MenuView : IDisposable {
	private const int WpfSize = 1024;
	private const int ResolutionMultiplier = 2;
	private const int PixelSize = WpfSize * ResolutionMultiplier;

	static MenuView() {
		RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
	}
	
	private class AsyncRenderer {
		private CountdownEvent readyEvent;
		private Dispatcher dispatcher;

		private MenuViewMessageInterpreter interpreter;
		private RenderTargetBitmap bitmap;

		public AsyncRenderer() {
			readyEvent = new CountdownEvent(1);

			var thread = new Thread(Init);
			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();

			readyEvent.Wait();
		}
	
		private void Init() {
			dispatcher = Dispatcher.CurrentDispatcher;
			readyEvent.Signal();

			interpreter = new MenuViewMessageInterpreter();
			bitmap = new RenderTargetBitmap(PixelSize, PixelSize, 96 * ResolutionMultiplier, 96 * ResolutionMultiplier, PixelFormats.Pbgra32);

			Dispatcher.Run();
		}

		private void RenderAndUpload(MenuViewMessage message, DataBox dataBox) {
			var	visual = interpreter.Interpret(message);
			visual.Arrange(new Rect(0, 0, WpfSize, WpfSize));
			visual.UpdateLayout();

			bitmap.Clear();
			bitmap.Render(visual);

			bitmap.CopyPixels(new Int32Rect(0, 0, PixelSize, PixelSize), dataBox.DataPointer, dataBox.SlicePitch, dataBox.RowPitch);
		}

		public Task DispatchRenderAndUpload(MenuViewMessage message, DataBox dataBox) {
			var operation = dispatcher.InvokeAsync(() => RenderAndUpload(message, dataBox));
			return operation.Task;
		}
	}
	
	private readonly MenuModel model;
	private readonly Texture2D stagingTexture;
	private readonly Texture2D texture;
	private readonly ShaderResourceView textureView;
	private readonly AsyncRenderer asyncRenderer;
	private readonly MenuViewMessageAuthor author;

	private bool wasChangedSinceLastUpdate = true;
	private Task renderAndUploadTask;

	public MenuView(Device device, MenuModel model) {
		this.model = model;

		stagingTexture = new Texture2D(device, new Texture2DDescription {
			Width = PixelSize,
			Height = PixelSize,
			ArraySize = 1,
			MipLevels = 1,
			Format = Format.B8G8R8A8_UNorm_SRgb,
			SampleDescription = new SampleDescription(1, 0),
			Usage = ResourceUsage.Staging,
			BindFlags = BindFlags.None,
			CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write
		});
		texture = new Texture2D(device, new Texture2DDescription {
			Width = PixelSize,
			Height = PixelSize,
			ArraySize = 1,
			MipLevels = 0,
			Format = Format.B8G8R8A8_UNorm_SRgb,
			SampleDescription = new SampleDescription(1, 0),
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
			OptionFlags = ResourceOptionFlags.GenerateMipMaps
		});
		textureView = new ShaderResourceView(device, texture);

		asyncRenderer = new AsyncRenderer();
		author = new MenuViewMessageAuthor();

		model.Changed += () => { wasChangedSinceLastUpdate = true; };
	}
	
	public void Dispose() {
		stagingTexture.Dispose();
		texture.Dispose();
		textureView.Dispose();
	}
	
	public ShaderResourceView TextureView => textureView;

	public void Update(DeviceContext context) {
		if (renderAndUploadTask != null) {
			if (!renderAndUploadTask.IsCompleted) {
				return;
			}

			if (renderAndUploadTask.Exception != null) {
				throw renderAndUploadTask.Exception;
			}
			
			renderAndUploadTask = null;
	
			context.UnmapSubresource(stagingTexture, 0);
				
			context.CopySubresourceRegion(stagingTexture, 0, null, texture, 0);
			context.GenerateMips(textureView);
		}

		if (renderAndUploadTask == null) {
			if (!wasChangedSinceLastUpdate) {
				return;
			}

			var dataBox = context.MapSubresource(stagingTexture, 0, 0, MapMode.ReadWrite, MapFlags.DoNotWait, out var stream);
			if (dataBox.IsEmpty) {
				return;
			}

			var message = author.AuthorMessage(model);
			renderAndUploadTask = asyncRenderer.DispatchRenderAndUpload(message, dataBox);
			wasChangedSinceLastUpdate = false;
		}
	}
}
