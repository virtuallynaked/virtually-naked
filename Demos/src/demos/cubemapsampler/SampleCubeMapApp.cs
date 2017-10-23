using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Reflection;
using Device = SharpDX.Direct3D11.Device;

public class SampleCubeMapApp : IDemoApp {
	public void Run() {
		Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug);
		ShaderCache shaderCache = new ShaderCache(device);

		var stamdardSamplers = new StandardSamplers(device);

		var dataDir = UnpackedArchiveDirectory.Make(CommonPaths.WorkDir);
		var environment = new ImageBasedLightingEnvironment(device, stamdardSamplers, dataDir, "ruins");
		
		DeviceContext context = device.ImmediateContext;
		
		Vector4[] samplePositions = {
			new Vector4(+1, 0, 0, 0),
			new Vector4(-1, 0, 0, 0),
			new Vector4(0, +1, 0, 0),
			new Vector4(0, -1, 0, 0),
			new Vector4(0, 0, +1, 0),
			new Vector4(0, 0, -1, 0),
		};
		
		var inBufferView = BufferUtilities.ToStructuredBufferView(device, samplePositions);

		ComputeShader shader = shaderCache.GetComputeShader<SampleCubeMapApp>("demos/cubemapsampler/SampleCubeMap");

		var outBuffer = new StageableStructuredBufferManager<Vector4>(device, samplePositions.Length);

		context.ComputeShader.Set(shader);
		environment.Apply(context.ComputeShader);
		context.ComputeShader.SetShaderResource(1, inBufferView);
		context.ComputeShader.SetUnorderedAccessView(0, outBuffer.View);

		context.Dispatch(samplePositions.Length, 1, 1);
		context.ClearState();

		Vector4[] results = outBuffer.ReadContents(context);

		for (int i = 0; i < samplePositions.Length; ++i) {
			Console.WriteLine(samplePositions[i]);
			Console.WriteLine("\t" + results[i].X);
			Console.WriteLine("\t" + results[i].Y);
			Console.WriteLine("\t" + results[i].Z);
			Console.WriteLine("\t" + results[i].W);
		}
	}
}

