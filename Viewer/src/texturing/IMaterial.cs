using System;
using SharpDX.Direct3D11;

public interface IMaterial : IDisposable {
	string UvSet { get; }

	void Apply(DeviceContext context, OutputMode outputMode, ShaderResourceView secondaryNormalMap);
	void Unapply(DeviceContext context);
}
