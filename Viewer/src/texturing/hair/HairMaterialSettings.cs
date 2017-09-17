using System;
using SharpDX.Direct3D11;
using ProtoBuf;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class HairMaterialSettings : IMaterialSettings {
	public string uvSet;

	public ColorTexture diffuseAlbedo;
	public FloatTexture opacity;
	
	public IMaterial Load(Device device, ShaderCache shaderCache, TextureLoader textureLoader) {
		return HairMaterial.Load(device, shaderCache, textureLoader, this);
	}
}
