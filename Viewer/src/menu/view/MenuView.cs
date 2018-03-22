using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
	private const int StagingTextureCount = 3;
	private const int WpfSize = 1024;
	private const int ResolutionMultiplier = 2;
	private const int PixelSize = WpfSize * ResolutionMultiplier;

	static MenuView() {
		RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
	}
	
	private class StagingTexture {
		public readonly Texture2D texture;
		public DataBox dataBox;

		private StagingTexture(Texture2D texture) {
			this.texture = texture;
			dataBox = new DataBox();
		}

		public void Dispose() {
			texture.Dispose();
		}

		public void UnmapAndCopy(DeviceContext context, Texture2D target) {
			dataBox = new DataBox();
			context.UnmapSubresource(texture, 0);
			context.CopySubresourceRegion(texture, 0, null, target, 0);
		}

		public bool TryMap(DeviceContext context) {
			dataBox = context.MapSubresource(texture, 0, 0, MapMode.ReadWrite, MapFlags.DoNotWait, out var stream);
			return !dataBox.IsEmpty;
		}

		public static StagingTexture Make(Device device) {
			var texture = new Texture2D(device, new Texture2DDescription {
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
			return new StagingTexture(texture);
		}
	}

	private readonly MenuModel model;
	private readonly Texture2D texture;
	private readonly ShaderResourceView textureView;
	private readonly MenuViewMessageAuthor author;

	private bool wasChangedSinceLastUpdate = true;
	
	private readonly List<StagingTexture> stagingTextures = new List<StagingTexture>();
	private readonly BlockingCollection<MenuViewMessage> messageQueue = new BlockingCollection<MenuViewMessage>();
	private readonly BlockingCollection<StagingTexture> readyToRenderToQueue = new BlockingCollection<StagingTexture>();
	private readonly ConcurrentQueue<StagingTexture> readyToCopyFromQueue = new ConcurrentQueue<StagingTexture>();
	private readonly ConcurrentQueue<StagingTexture> readyToMapQueue = new ConcurrentQueue<StagingTexture>();

	private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
	private readonly Thread renderThread;

	public MenuView(Device device, MenuModel model) {
		this.model = model;

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

		for (int i = 0; i < StagingTextureCount; ++i) {
			var stagingTexture = StagingTexture.Make(device);
			stagingTextures.Add(stagingTexture);
			readyToMapQueue.Enqueue(stagingTexture);
		}
		
		author = new MenuViewMessageAuthor();

		model.Changed += () => { wasChangedSinceLastUpdate = true; };

		renderThread = new Thread(MenuRenderProc);
		renderThread.SetApartmentState(ApartmentState.STA);
		renderThread.Start();
	}
	
	public void Dispose() {
		cancellationTokenSource.Cancel();
		renderThread.Join();
		
		cancellationTokenSource.Dispose();

		foreach (var stagingTexture in stagingTextures) {
			stagingTexture.Dispose();
		}

		texture.Dispose();
		textureView.Dispose();
	}
	
	private void MenuRenderProc() {
		var cancellationToken = cancellationTokenSource.Token;
		
		var interpreter = new MenuViewMessageInterpreter();
		var bitmap = new RenderTargetBitmap(PixelSize, PixelSize, 96 * ResolutionMultiplier, 96 * ResolutionMultiplier, PixelFormats.Pbgra32);

		while (!cancellationToken.IsCancellationRequested) {
			StagingTexture stagingTexture;
			MenuViewMessage message;
			try {
				stagingTexture = readyToRenderToQueue.Take(cancellationToken);
				message = messageQueue.Take(cancellationToken);
			} catch (OperationCanceledException) {
				break;
			}
			
			//if there are multiple pending messages, keep taking them until the queue is drained
			while (messageQueue.TryTake(out var secondMessage)) {
				message = secondMessage;
			}
			
			var	visual = interpreter.Interpret(message);
			visual.Arrange(new Rect(0, 0, WpfSize, WpfSize));
			visual.UpdateLayout();

			bitmap.Clear();
			bitmap.Render(visual);

			var dataBox = stagingTexture.dataBox;
			bitmap.CopyPixels(new Int32Rect(0, 0, PixelSize, PixelSize), dataBox.DataPointer, dataBox.SlicePitch, dataBox.RowPitch);
			readyToCopyFromQueue.Enqueue(stagingTexture);
		}
	}

	public ShaderResourceView TextureView => textureView;
			
	public void Update(DeviceContext context) {
		if (!wasChangedSinceLastUpdate) {
			return;
		}
		messageQueue.Add(author.AuthorMessage(model));
		wasChangedSinceLastUpdate = false;
	}

	public void DoPrework(DeviceContext context) {
		if (readyToCopyFromQueue.TryDequeue(out var stagingTexture)) {
			//if there are multiple pending textures, keep taking them until the queue is drained
			while (readyToMapQueue.TryDequeue(out var secondStagingTexture)) {
				readyToRenderToQueue.Add(stagingTexture);
				stagingTexture = secondStagingTexture;
			}
			
			stagingTexture.UnmapAndCopy(context, texture);
			context.GenerateMips(textureView);
			readyToMapQueue.Enqueue(stagingTexture);
		}
	}

	public void DoPostwork(DeviceContext context) {
		while (true) {
			if (!readyToMapQueue.TryPeek(out var stagingTexture)) {
				break;
			}
			
			if (!stagingTexture.TryMap(context)) {
				break;
			}

			if (!readyToMapQueue.TryDequeue(out var stagingTextureAgain) || stagingTextureAgain != stagingTexture) {
				throw new Exception("impossible");
			}
			
			readyToRenderToQueue.Add(stagingTexture);
		}
	}
}
