using SharpDX;
using SharpDX.Direct3D11;
using System;
using Valve.VR;

public interface IPreparedFrame : IDisposable {
	void DoPrework(DeviceContext context, TrackedDevicePose_t[] poses);
	Texture2D RenderView(DeviceContext context, HiddenAreaMesh mesh, Matrix viewTransform, Matrix projectionTransform);
	void DrawCompanionWindowUi(DeviceContext context);
	void DoPostwork(DeviceContext context);
}