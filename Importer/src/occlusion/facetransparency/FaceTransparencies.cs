using System.IO;
using System.Linq;

public static class FaceTransparencies {
	public static float[] For(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo figureDestDir) {
		if (surfaceProperties.MaterialSetForOpacities != null) {
			var materialSetDir = figureDestDir.Subdirectory("material-sets").Subdirectory(surfaceProperties.MaterialSetForOpacities);
			var transparenciesFile = materialSetDir.File("face-transparencies.array");
			return transparenciesFile.ReadArray<float>();
		}
		
		var surfaceMap = figure.Geometry.SurfaceMap;
		return surfaceMap
			.Select(surfaceIdx => 1 - surfaceProperties.Opacities[surfaceIdx])
			.ToArray();
	}
}
