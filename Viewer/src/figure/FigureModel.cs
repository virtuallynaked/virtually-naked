using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public class FigureModel {
	private readonly FigureDefinition definition;

	public FigureModel(FigureDefinition definition) {
		this.definition = definition;
	}

	public bool IsVisible { get; set; }
	public Shape Shape { get; set; }
	public MaterialSetAndVariantOption MaterialSetAndVariant { get; set; }
	
	public MaterialSetOption MaterialSet {
		get {
			return MaterialSetAndVariant.MaterialSet;
		}
		set {
			MaterialSetAndVariant = MaterialSetAndVariantOption.MakeWithDefaultVariants(value);
		}
	}

	public string ShapeName {
		get {
			return Shape?.Label;
		}
		set {
			var shape = definition.ShapeOptions.Find(option => option.Label == value);
			if (shape == null) {
				shape = definition.ShapeOptions.First();
			} 

			Shape = shape;
		}
	}
	
	public void SetMaterialSetAndVariantByName(string materialSetName, Dictionary<string, string> variantNames) {
		var materialSet = definition.MaterialSetOptions.Find(option => option.Label == materialSetName);
		if (materialSet == null) {
			materialSet = definition.MaterialSetOptions.First();
		}

		MaterialSetAndVariant = variantNames == null ?
			MaterialSetAndVariantOption.MakeWithDefaultVariants(materialSet) :
			new MaterialSetAndVariantOption(materialSet, variantNames.ToImmutableDictionary());
	}
}
