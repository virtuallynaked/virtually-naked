using System;
using System.Collections.Generic;
using System.Linq;

public class MaterialSetVariantMenuItem : IToggleMenuItem {
	private readonly FigureModel model;
	private readonly MultiMaterialSettings.VariantCategory category;
	private readonly MultiMaterialSettings.Variant variant;

	public MaterialSetVariantMenuItem(FigureModel model, MultiMaterialSettings.VariantCategory category, MultiMaterialSettings.Variant variant) {
		this.model = model;
		this.category = category;
		this.variant = variant;
	}

	public string Label => variant.Name;

	public bool IsSet {
		get {
			model.MaterialSetAndVariant.VariantSelections.TryGetValue(category.Name, out var activeVariantName);
			return activeVariantName == variant.Name;
		}
	}

	public void Toggle() {
		model.MaterialSetAndVariant = model.MaterialSetAndVariant.SetVariant(category, variant);
	}
}

public class MaterialSetVariantsMenuLevel : IMenuLevel {
	private readonly FigureDefinition definition;
	private readonly FigureModel model;

	public MaterialSetVariantsMenuLevel(FigureDefinition definition, FigureModel model) {
		this.definition = definition;
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}
	
	public List<IMenuItem> GetItems() {
		return model.MaterialSetAndVariant.MaterialSet.Settings.VariantCategories
			.Select(category => (IMenuItem) new SubLevelMenuItem(category.Name, MakeVariantMenuLevel(category)))
			.ToList();
	}

	private IMenuLevel MakeVariantMenuLevel(MultiMaterialSettings.VariantCategory category) {
		var items = category.Variants
			.Select(variant => new MaterialSetVariantMenuItem(model, category, variant))
			.ToArray();
		return new StaticMenuLevel(items);
	}
}
