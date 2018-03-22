using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ShaderSlots {
	// Texture Slots
	public const int DiffuseEnvironmentCube = 0;
	public const int GlossyEnvironmentCube = 1;
	public const int MaterialTextureStart = 2;

	// Constant Buffer Slots
	public const int EnvironmentParameters = 10;
	public const int MaterialConstantBufferStart = 0;
}
