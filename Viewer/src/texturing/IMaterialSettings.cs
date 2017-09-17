using ProtoBuf;
using SharpDX.Direct3D11;

[ProtoContract]
[ProtoInclude(1, typeof(UberMaterialSettings))]
[ProtoInclude(2, typeof(HairMaterialSettings))]
public interface IMaterialSettings {
	IMaterial Load(Device device, ShaderCache shaderCache, TextureLoader textureLoader);
}
