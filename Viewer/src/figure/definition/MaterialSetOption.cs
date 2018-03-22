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

	public static MaterialSetOption Load(IArchiveDirectory shapeDirectory) {
		return new MaterialSetOption(shapeDirectory.Name, shapeDirectory);
	}
	
	public string Label { get; }
	public IArchiveDirectory Directory { get; }

	public MaterialSetOption(string label, IArchiveDirectory directory) {
		Label = label;
		Directory = directory;
	}
}
