using System;
using System.Collections.Generic;

public enum MenuItemViewType {
	UpLevelButton,
	SubLevelButton,
	ActionButton,
	Range,
	Toggle
}

public class MenuItemViewMessage {
	public String Label { get; set; }
	public bool Active { get; set; }
	public MenuItemViewType Type { get; set; }

	//Range settings
	public double Min { get; set; }
	public double Max { get; set; }
	public double Value { get; set; }
	public bool IsEditing { get; set; }

	//Toggle Settings
	public bool IsSet { get; set; }

	public static MenuItemViewMessage MakeRange(string label, double min, double max, double value, bool isEditing) {
		return new MenuItemViewMessage {
			Label = label,
			Type = MenuItemViewType.Range,
			Min = min,
			Max = max,
			Value = value,
			IsEditing = isEditing
		};
	}

	public static MenuItemViewMessage MakeToggle(string label, bool isSet) {
		return new MenuItemViewMessage {
			Label = label,
			Type = MenuItemViewType.Toggle,
			IsSet = isSet
		};
	}
	
	public static MenuItemViewMessage MakeSubLevelButton(string label) {
		return new MenuItemViewMessage {
			Label = label,
			Type = MenuItemViewType.SubLevelButton
		};
	}
	
	public static MenuItemViewMessage MakeUpLevelButton(string label) {
		return new MenuItemViewMessage {
			Label = label,
			Type = MenuItemViewType.UpLevelButton
		};
	}
	
	public static MenuItemViewMessage MakeActionButton(string label, bool requiresMoreInformation) {
		return new MenuItemViewMessage {
			Label = label,
			Type = requiresMoreInformation ? MenuItemViewType.SubLevelButton : MenuItemViewType.ActionButton
		};
	}
}

public class MenuViewMessage {
	public List<MenuItemViewMessage> Items { get; set; } = new List<MenuItemViewMessage>();
	public float Position { get; set; }
}
