using System;
using System.Collections.Generic;

public class MaterialsModel {
	public static MaterialsModel Load(IArchiveDirectory figureDir, string initialMaterialSetName) {
		List<MaterialSetOption> options = new List<MaterialSetOption>();
		MaterialSetOption active = null;
		
		IArchiveDirectory materialSetsDirectory = figureDir.Subdirectory("material-sets");
		foreach (var materialSetDirectory in materialSetsDirectory.Subdirectories) {
			var materialSet = MaterialSetOption.Load(materialSetDirectory);
			options.Add(materialSet);
			if (materialSet.Label == initialMaterialSetName) {
				active = materialSet;
			}
		}
		
		return new MaterialsModel(options, active);
	}

	public List<MaterialSetOption> Options { get; }
	private MaterialSetOption active;
	
	public event Action<MaterialSetOption, MaterialSetOption> Changed;
	
	public MaterialsModel(List<MaterialSetOption> options, MaterialSetOption active) {
		Options = options;
		this.active = active;
	}
	
	public MaterialSetOption Active {
		get {
			return active;
		}
		set {
			var old = active;
			active = value;
			Changed?.Invoke(old, value);
		}
	}
}
