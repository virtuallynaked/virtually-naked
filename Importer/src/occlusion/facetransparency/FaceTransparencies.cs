using System.Linq;

public static class FaceTransparencies {
	public static float[] For(ImporterPathManager pathManager, Figure figure) {
		var surfaceProperties = SurfacePropertiesJson.Load(pathManager, figure);

		if (surfaceProperties.MaterialSetForOpacities != null) {
			var figureDir = pathManager.GetDestDirForFigure(figure.Name);
			var materialSetDir = figureDir.Subdirectory("material-sets").Subdirectory(surfaceProperties.MaterialSetForOpacities);
			var transparenciesFile = materialSetDir.File("face-transparencies.array");
			return transparenciesFile.ReadArray<float>();
		}
		
		var surfaceMap = figure.Geometry.SurfaceMap;
		return surfaceMap
			.Select(surfaceIdx => 1 - surfaceProperties.Opacities[surfaceIdx])
			.ToArray();
	}
}
