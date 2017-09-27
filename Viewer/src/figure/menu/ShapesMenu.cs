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

	public bool IsSet => model.Shapes.Active == shape;

	public void Toggle() {
		model.Shapes.Active = shape;
	}
}

public class ShapesMenuLevel : IMenuLevel {
	private readonly FigureModel model;

	public ShapesMenuLevel(FigureModel model) {
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return model.Shapes.Options
			.Select(shape => (IMenuItem) new ShapesMenuItem(model, shape))
			.ToList();
	}
}