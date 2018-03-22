using System;
using System.Collections.Generic;
using System.Linq;

public class MaterialSetMenuItem : IToggleMenuItem {
	private readonly FigureModel model;
	private readonly MaterialSetOption materialSet;

	public MaterialSetMenuItem(FigureModel model, MaterialSetOption materialSet) {
		this.model = model;
		this.materialSet = materialSet;
	}

	public string Label => materialSet.Label;

	public bool IsSet => model.MaterialSet == materialSet;

	public void Toggle() {
		model.MaterialSet = materialSet;
	}
}

public class MaterialsMenuLevel : IMenuLevel {
	private readonly FigureDefinition definition;
	private readonly FigureModel model;

	public MaterialsMenuLevel(FigureDefinition definition, FigureModel model) {
		this.definition = definition;
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return definition.MaterialSetOptions
			.Select(materialSet => (IMenuItem) new MaterialSetMenuItem(model, materialSet))
			.ToList();
	}
}
