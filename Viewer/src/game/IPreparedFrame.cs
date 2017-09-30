using SharpDX;
using SharpDX.Direct3D11;
using System;

public interface IPreparedFrame : IDisposable {
	void DoPrework(DeviceContext context);
	Texture2D RenderView(DeviceContext context, HiddenAreaMesh mesh, Matrix viewTransform, Matrix projectionTransform);
	void DrawCompanionWindowUi(DeviceContext context);
}