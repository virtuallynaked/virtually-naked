using System;
using SharpDX.Direct3D11;

public interface IOpaqueMaterial : IDisposable {
	void Apply(DeviceContext context);
}
