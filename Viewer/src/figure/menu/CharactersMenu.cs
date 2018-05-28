using System;
using System.Collections.Generic;

public class CharacterMenuItem : IToggleMenuItem {
	private readonly FigureModel model;
	private readonly Character character;

	public CharacterMenuItem(FigureModel model, Character character) {
		this.model = model;
		this.character = character;
	}

	public string Label => character.Label;

	public bool IsSet => model.Shape.Label == character.Shape && model.MaterialSetAndVariant.MaterialSet.Label == character.MaterialSet;

	public void Toggle() {
		model.ShapeName = character.Shape;
		model.SetMaterialSetAndVariantByName(character.MaterialSet, null);
	}
}

public class CharactersMenuLevel : IMenuLevel {
	private readonly List<IMenuItem> items;

	public CharactersMenuLevel(List<Character> characters, FigureDefinition definition, FigureModel model) {
		var shapesMenuLevel = new ShapesMenuLevel(definition, model);
		var materialsMenuLevel = new MaterialsMenuLevel(definition, model);
		var advancedMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Shape", shapesMenuLevel),
			new SubLevelMenuItem("Skin", materialsMenuLevel)
		);

		var detailsMenuLevel = new MaterialSetVariantsMenuLevel(definition, model);

		items = new List<IMenuItem>{};
		items.Add(new SubLevelMenuItem("Mix & Match", advancedMenuLevel));
		items.Add(new SubLevelMenuItem("Character Details", detailsMenuLevel));
		foreach (var character in characters) {
			items.Add(new CharacterMenuItem(model, character));
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
