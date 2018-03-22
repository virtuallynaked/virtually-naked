using System;
using System.Linq;

public class FigureModel {
	private readonly FigureDefinition definition;

	public FigureModel(FigureDefinition definition) {
		this.definition = definition;
	}

	private Shape shape;
	public event Action<Shape, Shape> ShapeChanged;
	public Shape Shape {
		get {
			return shape;
		}
		set {
			var old = shape;
			shape = value;
			ShapeChanged?.Invoke(old, value);
		}
	}

	private MaterialSetOption materialSet;
	public event Action<MaterialSetOption, MaterialSetOption> MaterialSetChanged;
	public MaterialSetOption MaterialSet {
		get {
			return materialSet;
		}
		set {
			var old = materialSet;
			materialSet = value;
			MaterialSetChanged?.Invoke(old, value);
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
