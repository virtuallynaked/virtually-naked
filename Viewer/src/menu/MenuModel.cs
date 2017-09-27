using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public interface IMenuLevel {
	List<IMenuItem> GetItems();
	event Action ItemsChanged;
}

public class StaticMenuLevel : IMenuLevel {
	private readonly List<IMenuItem> items;

	public StaticMenuLevel(params IMenuItem[] items) {
		this.items = items.ToList();
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return items;
	}
}

public class CombinedMenuLevel : IMenuLevel {
	private readonly IMenuLevel[] levels;

	public CombinedMenuLevel(params IMenuLevel[] levels) {
		this.levels = levels;
	}

	public event Action ItemsChanged {
		add {
			foreach (var level in levels) {
				level.ItemsChanged += value;
			}
		}
		remove {
			foreach (var level in levels) {
				level.ItemsChanged -= value;
			}
		}
	}

	public List<IMenuItem> GetItems() {
		return levels
			.SelectMany(level => level.GetItems())
			.ToList();
	}
}

public interface IMenuItem {
	string Label { get; }
}

public class SubLevelMenuItem : IMenuItem {
	public string Label { get; }
	public IMenuLevel Level { get; }
	
	public SubLevelMenuItem(string label, IMenuLevel level) {
		Label = label;
		Level = level;
	}
}

public class UpMenuItem : IMenuItem {
	public static readonly UpMenuItem Instance = new UpMenuItem();

	public string Label => "Up";
}

public interface IRangeMenuItem : IMenuItem {
	double Min { get; }
	double Max { get; }
	double Value { get; }

	void ResetValue();
	void SetValue(double value);
}

public interface IToggleMenuItem : IMenuItem {
	bool IsSet { get; }
	void Toggle();
}

public class ActionMenuItem : IMenuItem {
	public string Label { get; }
	public Action Action { get; }
	
	public ActionMenuItem(string label, Action action) {
		Label = label;
		Action = action;
	}
}

public class MenuModel {
	private const int VisibleItems = 4;
	
	private IMenuLevel currentlyActiveLevel = null;
	private Stack<IMenuLevel> levelStack = new Stack<IMenuLevel>();
	private List<IMenuItem> items;
	private float y;
	private int selectedItemIdx;
	
	private IRangeMenuItem activeRangeItem = null;
	private double lastPulsedValue;
	
	public MenuModel(IMenuLevel rootLevel) {
		levelStack.Push(rootLevel);
		ActivateLevel();
	}

	public event Action Changed;

	public float ScrollPosition => y;
	public int SelectedItemIdx => selectedItemIdx;
	public List<IMenuItem> Items => items;

	public bool IsEditing => activeRangeItem != null;
	
	private void ActivateLevel() {
		if (currentlyActiveLevel != null) {
			currentlyActiveLevel.ItemsChanged -= OnItemsChanged;
		}
		currentlyActiveLevel = levelStack.Peek();
		currentlyActiveLevel.ItemsChanged += OnItemsChanged;
		
		y = 0.5f;
		selectedItemIdx = 0;

		RefreshItems();
	}

	private void RefreshItems() {
		items = new List<IMenuItem>();

		if (levelStack.Count > 1) {
			items.Add(UpMenuItem.Instance);
		}
		
		items.AddRange(currentlyActiveLevel.GetItems());
		
		Changed?.Invoke();
	}

	private void OnItemsChanged() {
		RefreshItems();
	}

	public static double Clamp(double value, double min, double max) {
		if (value < min) {
			return min;
		} else if (value > max) {
			return max;
		} else {
			return value;
		}
	}
	
	public int Move(Vector2 delta) {
		if (activeRangeItem == null) {
			y = MathUtil.Clamp(y - VisibleItems / 2 * delta.Y, 0, items.Count);
			int oldSelectedItemIdx = selectedItemIdx;
			selectedItemIdx = (int) y;
			if (selectedItemIdx >= items.Count) {
				selectedItemIdx = items.Count - 1;
			}
			Changed?.Invoke();
			if (selectedItemIdx != oldSelectedItemIdx) {
				return 800;
			} else {
				return 0;
			}
		} else {
			double rawValue = activeRangeItem.Value;
			
			double range = (activeRangeItem.Max - activeRangeItem.Min);
			rawValue = Clamp(rawValue + delta.X * range / 2, activeRangeItem.Min, activeRangeItem.Max);

			activeRangeItem.SetValue(rawValue);
			
			Changed?.Invoke();

			if (Math.Abs(lastPulsedValue - rawValue) > 0.01) {
				lastPulsedValue = rawValue;
				return 100;
			} else {
				return 0;
			}
		}
	}

	public void DoneMove() {
		if (activeRangeItem == null) {
			y = selectedItemIdx + 0.5f;
			Changed?.Invoke();
		}
	}

	public void Press() {
		if (activeRangeItem != null) {
			activeRangeItem = null;
			Changed?.Invoke();
		} else {
			IMenuItem item = items[selectedItemIdx];
			switch (item) {
				case UpMenuItem upItem:
					levelStack.Pop();
					ActivateLevel();
					break;

				case SubLevelMenuItem subLevelItem:
					levelStack.Push(subLevelItem.Level);
					ActivateLevel();
					break;

				case IRangeMenuItem rangeItem:
					activeRangeItem = rangeItem;
					lastPulsedValue = rangeItem.Value;
					Changed?.Invoke();
					break;

				case IToggleMenuItem toggleItem:
					toggleItem.Toggle();
					Changed?.Invoke();
					break;

				case ActionMenuItem actionItem:
					actionItem.Action.Invoke();
					Changed?.Invoke();
					break;

				default:
					throw new InvalidOperationException();
			}
		}
	}
	
	public void PressSecondary() {
		if (activeRangeItem == null) {
			if (levelStack.Count > 1) {
				levelStack.Pop();
				ActivateLevel();
			}
		} else {
			activeRangeItem.ResetValue();
			activeRangeItem = null;
			Changed?.Invoke();
		}
	}
}
