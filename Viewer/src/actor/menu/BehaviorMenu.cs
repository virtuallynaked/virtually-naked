using System;
using System.Collections.Generic;

public class LookAtPlayerMenuItem : IToggleMenuItem {
	private readonly BehaviorModel model;

	public LookAtPlayerMenuItem(BehaviorModel model) {
		this.model = model;
	}

	public bool IsSet => model.LookAtPlayer;

	public string Label => "Look at Player";

	public void Toggle() {
		model.LookAtPlayer = !model.LookAtPlayer;
	}
}

public class BehaviorMenuLevel : IMenuLevel {
	private readonly BehaviorModel model;

	public BehaviorMenuLevel(BehaviorModel model) {
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return new List<IMenuItem> {
			new LookAtPlayerMenuItem(model)
		};
	}
}
