using System.Collections.Generic;

public class MaterialSetOption {
	public static List<MaterialSetOption> LoadAllForFigure(IArchiveDirectory figureDir) {
		List<MaterialSetOption> materialSets = new List<MaterialSetOption>();
		
		IArchiveDirectory materialSetsDirectory = figureDir.Subdirectory("material-sets");
		foreach (var materialSetDirectory in materialSetsDirectory.Subdirectories) {
			var materialSet = MaterialSetOption.Load(materialSetDirectory);
			materialSets.Add(materialSet);
		}
		
		return materialSets;
	}

	public static MaterialSetOption Load(IArchiveDirectory materialSetDirectory) {
		var settings = Persistance.Load<MultiMaterialSettings>(materialSetDirectory.File("material-settings.dat"));
		return new MaterialSetOption(materialSetDirectory.Name, materialSetDirectory, settings);
	}
	
	public string Label { get; }
	public IArchiveDirectory Directory { get; }
	public MultiMaterialSettings Settings { get; }

	public MaterialSetOption(string label, IArchiveDirectory directory, MultiMaterialSettings settings) {
		Label = label;
		Directory = directory;
		Settings = settings;
	}
}
