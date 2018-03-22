using System;
using System.Collections.Generic;
using System.Linq;

public class ShapesMenuItem : IToggleMenuItem {
	private readonly FigureModel model;
	private readonly Shape shape;

	public ShapesMenuItem(FigureModel model, Shape shape) {
		this.model = model;
		this.shape = shape;
	}

	public string Label => shape.Label;

	public bool IsSet => model.Shape == shape;

	public void Toggle() {
		model.Shape = shape;
	}
}

public class ShapesMenuLevel : IMenuLevel {
	private readonly FigureDefinition definition;
	private readonly FigureModel model;

	public ShapesMenuLevel(FigureDefinition definition, FigureModel model) {
		this.definition = definition;
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return definition.ShapeOptions
			.Select(shape => (IMenuItem) new ShapesMenuItem(model, shape))
			.ToList();
	}
}
