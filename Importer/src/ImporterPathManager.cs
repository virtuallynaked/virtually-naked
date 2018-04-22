using System.IO;

public class ImporterPathManager {
	public DirectoryInfo GetConfDirForFigure(string figureName) {
		return CommonPaths.ConfDir.Subdirectory(figureName);
	}
	
	public DirectoryInfo GetDestDirForFigure(string figureName) {
		return CommonPaths.WorkDir.Subdirectory("figures").Subdirectory(figureName);
	}
}
