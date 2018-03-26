using System.Collections.Generic;
using System.Linq;

public class CompositeToggleMenuItem : IToggleMenuItem {
	private readonly List<IToggleMenuItem> components;
	private readonly string label;

	public CompositeToggleMenuItem(string label, List<IToggleMenuItem> components) {
		this.label = label;
		this.components = components;
	}

	public string Label => label;

	public bool IsSet => components.All(component => component.IsSet);

	public void Toggle() {
		foreach (var component in components) {
			component.Toggle();
		}
	}

	public static List<IMenuItem> CombineByLabel(List<IToggleMenuItem> items) {
		var compositeItems = items
			.GroupBy(item => item.Label, (label, components) => new CompositeToggleMenuItem(label, components.ToList()))
			.ToList<IMenuItem>();
		return compositeItems;
	}
}
