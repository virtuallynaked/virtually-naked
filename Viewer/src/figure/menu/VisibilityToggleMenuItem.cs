public class VisibilityToggleMenuItem : IToggleMenuItem {
	private readonly string label;
	private readonly FigureModel model;

	public VisibilityToggleMenuItem(string label, FigureModel model) {
		this.label = label;
		this.model = model;
	}

	public bool IsSet => model.IsVisible;

	public string Label => label;

	public void Toggle() {
		model.IsVisible = !model.IsVisible;
	}
}
