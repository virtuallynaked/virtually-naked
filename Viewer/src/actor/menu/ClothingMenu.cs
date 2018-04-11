using System;
using System.Collections.Generic;
using System.Linq;

public class OutfitMenuItem : IToggleMenuItem {
	private readonly Actor actor;
	private readonly Outfit outfit;

	public OutfitMenuItem(Actor actor, Outfit outfit) {
		this.actor = actor;
		this.outfit = outfit;
	}

	public string Label => outfit.Label;

	public bool IsSet => outfit.IsMatch(actor.Clothing);

	public void Toggle() {
		var recipes = outfit.Items
			.Select(item => {
				InitialSettings.MaterialSets.TryGetValue(item.Figure, out string initialMaterialSetName);
				return new FigureFacade.Recipe {
					name = item.Figure,
					isVisible = item.IsInitiallyVisible,
					materialSet = initialMaterialSetName
				};
			})
			.ToArray();

		actor.SetClothing(recipes);
	}
}

public class ClothingMenuLevel : IMenuLevel {
	public static IMenuLevel Make(Actor actor) {
		return new ClothingMenuLevel(actor);
	}

	private readonly Actor actor;

	public ClothingMenuLevel(Actor actor) {
		this.actor = actor;
	}
	
	public event Action ItemsChanged {
		add { actor.ClothingChanged += value; }
		remove { actor.ClothingChanged -= value; }
	}

	public List<IMenuItem> GetItems() {
		var outfitsMenuLevel = MakeOutfitsMenuLevel();
		var materialsMenuLevel = MakeMaterialsMenuLevel();

		var items = new List<IMenuItem> { };
		items.Add(new SubLevelMenuItem("Outfits", outfitsMenuLevel));
		items.Add(new SubLevelMenuItem("Fabrics", materialsMenuLevel));

		var clothingFigures = actor.Clothing;
		var activeOutfit = actor.Outfits.Find(outfit => outfit.IsMatch(clothingFigures));

		for (int clothingIdx = 0; clothingIdx < clothingFigures.Length; ++clothingIdx) {
			var clothingFigure = clothingFigures[clothingIdx];
			var label = activeOutfit?.Items[clothingIdx].Label ?? clothingFigure.Definition.Name;
			items.Add(new VisibilityToggleMenuItem(label, clothingFigure.Model));
		}
		
		return items;
	}
	
	public IMenuLevel MakeMaterialsMenuLevel() {
		var individualMaterialItems = actor.Clothing
			.SelectMany(figure => figure.Definition.MaterialSetOptions
				.Select(materialSet => new MaterialSetMenuItem(figure.Model, materialSet)))
			.ToList<IToggleMenuItem>();
		var compositeMaterialItems = CompositeToggleMenuItem.CombineByLabel(individualMaterialItems);
		var materialsMenuLevel = new StaticMenuLevel(compositeMaterialItems.ToArray());
		return materialsMenuLevel;
	}

	public IMenuLevel MakeOutfitsMenuLevel() {
		var items = actor.Outfits
			.Select(outfit => new OutfitMenuItem(actor, outfit))
			.ToArray();
		return new StaticMenuLevel(items);
	}
}
