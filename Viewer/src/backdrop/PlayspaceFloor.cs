using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using Valve.VR;
using Device = SharpDX.Direct3D11.Device;

class PlayspaceFloor {
	[StructLayout(LayoutKind.Explicit, Size = 4 * 16)]
	private struct PlayAreaRect {
		[FieldOffset(0 * 16)] public Vector3 corner0;
		[FieldOffset(1 * 16)] public Vector3 corner1;
		[FieldOffset(2 * 16)] public Vector3 corner2;
		[FieldOffset(3 * 16)] public Vector3 corner3;
	}

	private readonly VertexShader vertexShader;
	private readonly PixelShader pixelShader;
	
	private ConstantBufferManager<PlayAreaRect> playAreaRectBuffer;

	public PlayspaceFloor(Device device, ShaderCache shaderCache) {
		vertexShader = shaderCache.GetVertexShader<Backdrop>("backdrop/PlayspaceFloor");
		pixelShader = shaderCache.GetPixelShader<Backdrop>("backdrop/PlayspaceFloor");
		playAreaRectBuffer = new ConstantBufferManager<PlayAreaRect>(device);
	}

	public void Dispose() {
		playAreaRectBuffer.Dispose();
	}
	
	public void Update(DeviceContext context) {
		PlayAreaRect value;

		HmdQuad_t rect = default(HmdQuad_t);
		if (OpenVR.Chaperone.GetPlayAreaRect(ref rect)) {
			value = new PlayAreaRect {
				corner0 = rect.vCorners0.Convert(),
				corner1 = rect.vCorners1.Convert(),
				corner2 = rect.vCorners3.Convert(),
				corner3 = rect.vCorners2.Convert()
			};
		} else {
			float halfSize = 1.5f;
			value = new PlayAreaRect {
				corner0 = new Vector3(-halfSize, 0, +halfSize),
				corner1 = new Vector3(+halfSize, 0, +halfSize),
				corner2 = new Vector3(-halfSize, 0, -halfSize),
				corner3 = new Vector3(+halfSize, 0, -halfSize),
			};
		}
		
		playAreaRectBuffer.Update(context, value);
	}

	public void Render(DeviceContext context) {
		context.VertexShader.Set(vertexShader);
		context.VertexShader.SetConstantBuffer(1, playAreaRectBuffer.Buffer);
		context.PixelShader.Set(pixelShader);
		context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		context.Draw(4, 0);
	}
}
