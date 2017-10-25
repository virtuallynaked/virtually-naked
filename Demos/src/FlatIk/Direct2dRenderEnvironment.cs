using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;

namespace FlatIk {
	public class WindowedDirect2dRenderEnvironment : IDisposable {
		private readonly RenderForm form;

		private readonly SharpDX.Direct3D11.Device d3dDevice;

		private readonly SharpDX.DXGI.Factory1 dxgiFactory;
		private readonly SharpDX.DXGI.Device dxgiDevice;
		private readonly SwapChain swapChain;
		private readonly Surface dxgiSurface;

		private readonly SharpDX.Direct2D1.Factory1 d2dFactory;
		private readonly SharpDX.Direct2D1.Device d2dDevice;
		private readonly SharpDX.Direct2D1.DeviceContext d2dContext;
		private readonly Bitmap1 bitmap;

		public WindowedDirect2dRenderEnvironment(string formName, bool debug) {
			form = new RenderForm(formName);

			d3dDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport | (debug ? DeviceCreationFlags.Debug : DeviceCreationFlags.None));
			
			dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>();
			dxgiFactory = new SharpDX.DXGI.Factory1();
			swapChain = new SwapChain(dxgiFactory, dxgiDevice, new SwapChainDescription {
				BufferCount = 1,
				ModeDescription = new ModeDescription(Format.B8G8R8A8_UNorm),
				OutputHandle = form.Handle,
				IsWindowed = true,
				SampleDescription = new SampleDescription(1, 0),
				SwapEffect = SwapEffect.Discard,
				Usage = Usage.RenderTargetOutput
			});
			dxgiSurface = swapChain.GetBackBuffer<Surface>(0);
			
			d2dFactory = new SharpDX.Direct2D1.Factory1(FactoryType.SingleThreaded, debug ? DebugLevel.Warning : DebugLevel.None);
			d2dDevice = new SharpDX.Direct2D1.Device(d2dFactory, dxgiDevice);
			d2dContext = new SharpDX.Direct2D1.DeviceContext(d2dDevice, DeviceContextOptions.None);
			bitmap = new Bitmap1(d2dContext, dxgiSurface, null);
			d2dContext.Target = bitmap;
		}

		public SharpDX.Direct2D1.DeviceContext D2dContext => d2dContext;

		public Size2F Size => bitmap.Size;

		public void Dispose() {
			d2dContext.Target = null;
			bitmap.Dispose();
			d2dContext.Dispose();
			d2dDevice.Dispose();
			d2dFactory.Dispose();
			
			dxgiSurface.Dispose();
			swapChain.Dispose();
			dxgiFactory.Dispose();
			dxgiDevice.Dispose();

			d3dDevice.Dispose();
			
			form.Dispose();
		}

		public void Run(Action renderCallback) {
			RenderLoop.Run(form, () => {
				d2dContext.BeginDraw();
				renderCallback.Invoke();
				d2dContext.EndDraw();
				swapChain.Present(0, PresentFlags.None);
			});
		}
	}
}