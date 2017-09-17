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

	public bool IsSet => model.Shapes.Active == shape && model.Materials.Active == materialSet;

	public void Toggle() {
		model.Shapes.Active = shape;
		model.Materials.Active = materialSet;
	}
}

public class CharactersMenuLevel : IMenuLevel {
	private readonly List<IMenuItem> items;

	public CharactersMenuLevel(FigureModel model) {
		var materialSetDict = model.Materials.Options.ToDictionary(item => item.Label, item => item);

		var shapesMenuLevel = new ShapesMenuLevel(model);
		var materialsMenuLevel = new MaterialsMenuLevel(model.Materials);
		var advancedMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Shape", shapesMenuLevel),
			new SubLevelMenuItem("Skin", materialsMenuLevel)
		);

		items = new List<IMenuItem>{};
		items.Add(new SubLevelMenuItem("Mix & Match", advancedMenuLevel));
		foreach (var shape in model.Shapes.Options) {
			if (materialSetDict.TryGetValue(shape.Label, out var materialSet)) {
				items.Add(new CharacterMenuItem(model, shape, materialSet));
			}
		}
	}

	public List<IMenuItem> GetItems() {
		return items;
	}
}