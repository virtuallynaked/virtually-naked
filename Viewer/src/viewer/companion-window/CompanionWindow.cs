using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using Device = SharpDX.Direct3D11.Device;
using System.Windows.Forms;
using SharpDX.Direct3D;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Diagnostics;

class CompanionWindow : IDisposable {
	private const float DesiredFov = 0.540306f; //2 * Atan[frame-width / 2, focal-length] where frame-width = 36mm and focal-length = 65mm (in radius)

	[StructLayout(LayoutKind.Sequential)]
	private struct AspectRatios {
		public float companionWindowAspectRatio;
		public float sourceAspectRatio;
	}

	private readonly Device device;
	private readonly StandardSamplers standardSamplers;
	private readonly RenderForm form;
	private readonly SwapChain swapChain;
	private RenderTargetView backBufferView;
	private Viewport viewport;
	private float aspectRatio;
	private readonly VertexShader copyFromSourceVertexShader;
	private readonly PixelShader copyFromSourcePixelShader;
	private readonly ConstantBufferManager<AspectRatios> aspectRatiosBufferManager;
	private readonly BlendState uiBlendState;
	private readonly Overlay overlay;

	private bool shareHmdView = true;
	private Vector3 viewPosition = new Vector3(0, -1.5f, -1f);
	private Vector3 viewRotation = new Vector3(0, 0, 0);

	private bool isDragging = false;
	private System.Drawing.Point dragStartPoint;
	private System.Drawing.Point dragCurrentPoint;

	public CompanionWindow(Device device, ShaderCache shaderCache, StandardSamplers standardSamplers, string title, IArchiveDirectory dataDir) {
		this.device = device;
		this.standardSamplers = standardSamplers;

		form = new RenderForm(title);
		
		// SwapChain description
		var desc = new SwapChainDescription() {
			BufferCount = 1,
			ModeDescription =
				new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
									default(Rational), Format.R8G8B8A8_UNorm_SRgb),
			IsWindowed = true,
			OutputHandle = form.Handle,
			SampleDescription = new SampleDescription(1, 0),
			SwapEffect = SwapEffect.Discard, //TODO: consider using flip
			Usage = Usage.RenderTargetOutput
		};
		
		using (var factory = new Factory1()) {
			factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
			swapChain = new SwapChain(factory, device, desc);
		}

		form.UserResized += OnUserResized;
		
		SetupBackbufferAndViewport();

		copyFromSourceVertexShader = shaderCache.GetVertexShader<CompanionWindow>("viewer/companion-window/CopyFromSource");
		copyFromSourcePixelShader = shaderCache.GetPixelShader<CompanionWindow>("viewer/companion-window/CopyFromSource");

		uiBlendState = MakeUiBlendState(device);

		aspectRatiosBufferManager = new ConstantBufferManager<AspectRatios>(device);
		
		overlay = Overlay.Load(device, shaderCache, dataDir.Subdirectory("ui").File("put-on-headset-overlay.dds"));

		form.KeyPress += OnKeyPress;
		form.MouseDown += OnMouseDown;
		form.MouseUp += OnMouseUp;
		form.MouseMove += OnMouseMove;


		DebugInitialize();
	}
	
	[Conditional("DEBUG")]
	private void DebugInitialize() {
		shareHmdView = false;
	}

	private static BlendState MakeUiBlendState(Device device) {
		BlendStateDescription desc = BlendStateDescription.Default();
		desc.RenderTarget[0].IsBlendEnabled = true;
		desc.RenderTarget[0].SourceBlend = BlendOption.One;
		desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
		return new BlendState(device, desc);
	}
	
	public void Dispose() {
		overlay.Dispose();
		aspectRatiosBufferManager.Dispose();
		uiBlendState.Dispose();
		backBufferView.Dispose();
		swapChain.Dispose();
		form.Dispose();
	}

	public Form Form => form;
	
	public bool HasIndependentCamera => !shareHmdView;
	public Vector3 CameraPosition => -viewPosition;

	public Matrix GetViewTransform() {
		UpdatePosition();
		return Matrix.Translation(viewPosition) * Matrix.RotationY(viewRotation.Y) * Matrix.RotationX(viewRotation.X);
	}

	private void OnUserResized(object sender, EventArgs eventArgs) {
		backBufferView.Dispose();

		SwapChainDescription currentDesc = swapChain.Description;
		swapChain.ResizeBuffers(currentDesc.BufferCount, form.ClientSize.Width, form.ClientSize.Height, currentDesc.ModeDescription.Format, currentDesc.Flags);

		SetupBackbufferAndViewport();
	}

	private void SetupBackbufferAndViewport() {
		using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0)) {
			backBufferView = new RenderTargetView(device, backBuffer);
		}

		System.Drawing.Rectangle clientRect = form.ClientRectangle;

		viewport = new Viewport(
			0, 0,
			clientRect.Width, clientRect.Height,
			0, 1);

		aspectRatio = (float) clientRect.Width / clientRect.Height;
	}
	
	public Matrix GetDesiredProjectionMatrix() {
		return Matrix.PerspectiveFovRH(DesiredFov, aspectRatio, RenderingConstants.ZNear, RenderingConstants.ZFar);
	}

	public void Display(ShaderResourceView sourceView, Matrix sourceProjectionMatrix, Action renderUi) {
		var context = device.ImmediateContext;

		float sourceAspectRatio = sourceProjectionMatrix.M22 / sourceProjectionMatrix.M11;
		
		aspectRatiosBufferManager.Update(context, new AspectRatios {
			companionWindowAspectRatio = aspectRatio,
			sourceAspectRatio = sourceAspectRatio
		});

		context.ClearState();
		context.Rasterizer.SetViewport(viewport);
		context.OutputMerger.SetRenderTargets(backBufferView);

		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		context.VertexShader.Set(copyFromSourceVertexShader);
		context.VertexShader.SetConstantBuffer(0, aspectRatiosBufferManager.Buffer);

		context.PixelShader.Set(copyFromSourcePixelShader);
		standardSamplers.Apply(context.PixelShader);
		context.PixelShader.SetShaderResource(0, sourceView);

		context.Draw(4, 0);

		context.OutputMerger.SetBlendState(uiBlendState);
		renderUi();
		if (shareHmdView) {
			overlay.Draw(context);
		}
		
		swapChain.Present(0, 0);
	}
	
	private void OnKeyPress(object sender, KeyPressEventArgs e) {
		if (e.KeyChar == ' ') {
			shareHmdView = !shareHmdView;
		}
	}
	
	private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
		isDragging = true;
		dragStartPoint = e.Location;
		dragCurrentPoint = e.Location;
	}

	private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
		isDragging = false;
	}

	private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
		dragCurrentPoint = e.Location;
	}

	private void UpdatePosition() {
		if (isDragging) {
			viewRotation.Y += (dragStartPoint.X - dragCurrentPoint.X) * 0.005f;
			viewRotation.X += (dragStartPoint.Y - dragCurrentPoint.Y) * 0.005f;

			dragStartPoint = dragCurrentPoint;
		}

		if (!form.Focused) {
			return;
		}

		Vector3 positionDelta = Vector3.Zero;

		float moveSpeed = 0.005f;
		if (Keyboard.GetKeyStates(Key.LeftShift).HasFlag(KeyStates.Down)) {
			moveSpeed /= 10;
		}
				
		if (Keyboard.GetKeyStates(Key.A).HasFlag(KeyStates.Down)) {
			positionDelta.X += moveSpeed;
		}
		if (Keyboard.GetKeyStates(Key.D).HasFlag(KeyStates.Down)) {
			positionDelta.X -= moveSpeed;
		}

		if (Keyboard.GetKeyStates(Key.C).HasFlag(KeyStates.Down)) {
			positionDelta.Y += moveSpeed;
		}
		if (Keyboard.GetKeyStates(Key.E).HasFlag(KeyStates.Down)) {
			positionDelta.Y -= moveSpeed;
		}

		if (Keyboard.GetKeyStates(Key.W).HasFlag(KeyStates.Down)) {
			positionDelta.Z += moveSpeed;
		}
		if (Keyboard.GetKeyStates(Key.S).HasFlag(KeyStates.Down)) {
			positionDelta.Z -= moveSpeed;
		}
		
		viewPosition += Vector3.TransformNormal(positionDelta, Matrix.RotationY(-viewRotation.Y));
	}
}