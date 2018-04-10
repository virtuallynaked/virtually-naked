using System;
using SharpDX.Direct3D11;

public interface IMaterial : IDisposable {
	string UvSet { get; }

	void Apply(DeviceContext context, OutputMode outputMode);
	void Unapply(DeviceContext context);
}
