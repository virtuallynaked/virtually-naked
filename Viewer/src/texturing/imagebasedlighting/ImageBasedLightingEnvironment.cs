using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

public class ImageBasedLightingEnvironment : IDisposable {
	[StructLayout(LayoutKind.Explicit, Size = 12 * 4)]
	private struct ShaderConstants {
		[FieldOffset(0 * 4)] public Vector3 rotationColumn1;
		[FieldOffset(4 * 4)] public Vector3 rotationColumn2;
		[FieldOffset(8 * 4)] public Vector3 rotationColumn3;
	}

	private static readonly Matrix3x3 InitialTransform = new Matrix3x3(0, 0, 1, 0, 1, 0, 1, 0, 0);
	
	private readonly Device device;
	private readonly StandardSamplers standardSamplers;
	private readonly IArchiveDirectory dataDir;

	private readonly ConstantBufferManager<ShaderConstants> constantBufferManager;
	
	private string environmentName;
	private ShaderResourceView diffuseEnvironmentCube;
	private ShaderResourceView glossyEnvironmentCube;

	public float Rotation { get; set; } = 0;
	
	private static ShaderResourceView LoadTexture(Device device, IArchiveFile file) {
		using (var dataView = file.OpenDataView()) {
			DdsLoader.CreateDDSTextureFromMemory(device, dataView.DataPointer, out Resource texture, out ShaderResourceView view);
			texture.Dispose();
			return view;
		}
	}

	public ImageBasedLightingEnvironment(Device device, StandardSamplers standardSamplers, IArchiveDirectory dataDir, string environmentName) {
		this.device = device;
		this.standardSamplers = standardSamplers;
		this.dataDir = dataDir;
		
		constantBufferManager = new ConstantBufferManager<ShaderConstants>(device);

		EnvironmentName = environmentName;

	}

	public void Dispose() {
		constantBufferManager.Dispose();
		diffuseEnvironmentCube?.Dispose();
		glossyEnvironmentCube?.Dispose();
	}

	public string EnvironmentName {
		get {
			return environmentName;
		}
		set {
			diffuseEnvironmentCube?.Dispose();
			glossyEnvironmentCube?.Dispose();

			environmentName = value;

			IArchiveDirectory environmentDir = dataDir.Subdirectory("environments").Subdirectory(environmentName);
			diffuseEnvironmentCube = LoadTexture(device, environmentDir.File("diffuse.dds"));
			glossyEnvironmentCube = LoadTexture(device, environmentDir.File("glossy.dds"));
		}
	}

	public void Predraw(DeviceContext context) {
		Matrix3x3 rotation = InitialTransform * Matrix3x3.RotationY(Rotation);
		constantBufferManager.Update(context, new ShaderConstants {
			rotationColumn1 = rotation.Column1,
			rotationColumn2 = rotation.Column2,
			rotationColumn3 = rotation.Column3
		});
	}

	public void Apply(CommonShaderStage stage) {
		standardSamplers.Apply(stage);
		stage.SetConstantBuffer(ShaderSlots.EnvironmentParameters, constantBufferManager.Buffer);
		stage.SetShaderResource(ShaderSlots.DiffuseEnvironmentCube, diffuseEnvironmentCube);
		stage.SetShaderResource(ShaderSlots.GlossyEnvironmentCube, glossyEnvironmentCube);
	}
}
