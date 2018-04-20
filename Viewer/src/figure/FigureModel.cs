using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

public class FigureModel {
	private readonly FigureDefinition definition;

	public FigureModel(FigureDefinition definition) {
		this.definition = definition;
	}

	public bool IsVisible { get; set; }
	public Shape Shape { get; set; }
	public MaterialSetOption MaterialSet { get; set; }
	public ImmutableDictionary<string, string> MaterialSetVariants { get; set; } = ImmutableDictionary<string, string>.Empty;
	
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

	public string MaterialSetName {
		get {
			return MaterialSet?.Label;
		}
		set {
			var materialSet = definition.MaterialSetOptions.Find(option => option.Label == value);
			if (materialSet == null) {
				materialSet = definition.MaterialSetOptions.First();
			} 

			MaterialSet = materialSet;
		}
	}

}
