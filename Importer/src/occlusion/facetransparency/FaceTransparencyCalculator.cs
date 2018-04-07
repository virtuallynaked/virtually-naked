using SharpDX.Direct3D11;
using System.Linq;

public static class FaceTransparencyCalculator {
	public static float[] Calculate(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure figure, int[] surfaceMap) {
		var surfaceProperties = SurfacePropertiesJson.Load(figure);

		if (surfaceProperties.MaterialSetForOpacities != null) {
			var figureDir = CommonPaths.WorkDir.Subdirectory("figures").Subdirectory(figure.Name);
			var materialSetDir = figureDir.Subdirectory("material-sets").Subdirectory(surfaceProperties.MaterialSetForOpacities);
			var transparenciesFile = materialSetDir.File("face-transparencies.array");
			return transparenciesFile.ReadArray<float>();
		}
		
		return surfaceMap
			.Select(surfaceIdx => 1 - surfaceProperties.Opacities[surfaceIdx])
			.ToArray();
	}
}
