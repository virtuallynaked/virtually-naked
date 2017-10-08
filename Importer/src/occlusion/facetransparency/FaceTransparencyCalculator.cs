using SharpDX.Direct3D11;
using System.Linq;

public static class FaceTransparencyCalculator {
	public static float[] Calculate(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure figure, int[] surfaceMap) {
		if (figure.Name == "liv-hair") {
			var calculator = new FromTextureFaceTransparencyCalculator(fileLocator, device, shaderCache, figure);
			return calculator.CalculateSurfaceTransparencies();
		}

		var surfaceProperties = SurfacePropertiesJson.Load(figure);
		
		return surfaceMap
			.Select(surfaceIdx => 1 - surfaceProperties.Opacities[surfaceIdx])
			.ToArray();
	}
}
