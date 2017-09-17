using System;
using SharpDX.Direct3D11;

public interface IMaterial : IDisposable {
	bool IsTransparent { get; }
	string UvSet { get; }

	void Apply(DeviceContext context, RenderingPass pass);
	void Unapply(DeviceContext context);
}
