using System.Collections.Generic;
using System.Linq;

public class MaterialSetMenuItem : IToggleMenuItem {
	private readonly MaterialsModel model;
	private readonly MaterialSetOption materialSet;

	public MaterialSetMenuItem(MaterialsModel model, MaterialSetOption materialSet) {
		this.model = model;
		this.materialSet = materialSet;
	}

	public string Label => materialSet.Label;

	public bool IsSet => model.Active == materialSet;

	public void Toggle() {
		model.Active = materialSet;
	}
}

public class MaterialsMenuLevel : IMenuLevel {
	private readonly MaterialsModel model;

	public MaterialsMenuLevel(MaterialsModel model) {
		this.model = model;
	}

	public List<IMenuItem> GetItems() {
		return model.Options
			.Select(materialSet => (IMenuItem) new MaterialSetMenuItem(model, materialSet))
			.ToList();
	}
}