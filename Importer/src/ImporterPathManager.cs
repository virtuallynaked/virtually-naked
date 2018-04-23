using System.Collections.Immutable;
using System.IO;

public class ImporterPathManager {
	private readonly ImmutableDictionary<string, string> figureNameToContentPackNameDict;

	public static ImporterPathManager Make(ImmutableList<ContentPackImportConfiguration> contentPacks) {
		var figureNameToPrimaryContentPackNameDictBuilder = ImmutableDictionary.CreateBuilder<string, string>();

		foreach (var contentPack in contentPacks) {
			foreach (var figure in contentPack.Figures) {
				if (figure.IsPrimary) {
					figureNameToPrimaryContentPackNameDictBuilder.Add(figure.Name, contentPack.Name);
				}
			}
		}

		return new ImporterPathManager(figureNameToPrimaryContentPackNameDictBuilder.ToImmutable());
	}

	public ImporterPathManager(ImmutableDictionary<string, string> figureNameToContentPackNameDict) {
		this.figureNameToContentPackNameDict = figureNameToContentPackNameDict;
	}
	
	public DirectoryInfo GetConfDirForFigure(string figureName) {
		var contentPackName = figureNameToContentPackNameDict[figureName];
		return CommonPaths.ConfDir.Subdirectory(contentPackName).Subdirectory("figures").Subdirectory(figureName);
	}
}
