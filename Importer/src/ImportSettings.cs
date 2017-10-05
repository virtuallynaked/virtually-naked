using System.Collections.Generic;

public class ImportSettings {
	public static ImportSettings MakeFromViewerInitialSettings() {
		return new ImportSettings();
	}

	public List<string> ChildFiguresToImport {
		get {
			List<string> childFiguresNames = new List<string>();
			if (InitialSettings.Hair != null) {
				childFiguresNames.Add(InitialSettings.Hair);
			}
			childFiguresNames.AddRange(InitialSettings.Clothing);
			return childFiguresNames;
		}
	}

	public bool ShouldImportEnvironment(string name) {
		return name == InitialSettings.Environment;
	}

	public bool ShouldImportMaterialSet(string figureName, string materialSetName) {
		InitialSettings.MaterialSets.TryGetValue(figureName, out string activeMaterialSetName);
		return materialSetName == activeMaterialSetName;
	}

	public bool ShouldImportShape(string figureName, string shapeName) {
		InitialSettings.Shapes.TryGetValue(figureName, out string activeShapeName);
		return shapeName == activeShapeName;
	}
}