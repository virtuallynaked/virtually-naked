using System;
using System.Collections.Generic;
using System.Linq;

public class CharacterMenuItem : IToggleMenuItem {
	private readonly FigureModel model;
	private readonly Shape shape;
	private readonly MaterialSetOption materialSet;

	public CharacterMenuItem(FigureModel model, Shape shape, MaterialSetOption materialSet) {
		this.model = model;
		this.shape = shape;
		this.materialSet = materialSet;
	}

	public string Label => shape.Label;

	public bool IsSet => model.Shape == shape && model.MaterialSet == materialSet;

	public void Toggle() {
		model.Shape = shape;
		model.MaterialSet = materialSet;
	}
}

public class CharactersMenuLevel : IMenuLevel {
	private readonly List<IMenuItem> items;

	public CharactersMenuLevel(FigureDefinition definition, FigureModel model) {
		var materialSetDict = definition.MaterialSetOptions.ToDictionary(item => item.Label, item => item);

		var shapesMenuLevel = new ShapesMenuLevel(definition, model);
		var materialsMenuLevel = new MaterialsMenuLevel(definition, model);
		var advancedMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Shape", shapesMenuLevel),
			new SubLevelMenuItem("Skin", materialsMenuLevel)
		);

		items = new List<IMenuItem>{};
		items.Add(new SubLevelMenuItem("Mix & Match", advancedMenuLevel));
		foreach (var shape in definition.ShapeOptions) {
			if (materialSetDict.TryGetValue(shape.Label, out var materialSet)) {
				items.Add(new CharacterMenuItem(model, shape, materialSet));
			}
		}
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return items;
	}
}
