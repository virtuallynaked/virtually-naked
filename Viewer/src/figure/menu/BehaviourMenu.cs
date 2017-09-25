using System.Collections.Generic;

public class LookAtPlayerMenuItem : IToggleMenuItem {
	private readonly BehaviourModel model;

	public LookAtPlayerMenuItem(BehaviourModel model) {
		this.model = model;
	}

	public bool IsSet => model.LookAtPlayer;

	public string Label => "Look at Player";

	public void Toggle() {
		model.LookAtPlayer = !model.LookAtPlayer;
	}
}

public class BehaviourMenuLevel : IMenuLevel {
	private readonly BehaviourModel model;

	public BehaviourMenuLevel(BehaviourModel model) {
		this.model = model;
	}

	public List<IMenuItem> GetItems() {
		return new List<IMenuItem> {
			new LookAtPlayerMenuItem(model)
		};
	}
}
