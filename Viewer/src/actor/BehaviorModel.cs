public class BehaviorModel {
	public bool LookAtPlayer { get; set; } = true;

	public class Recipe {
		public bool? lookAtPlayer;

		public void Merge(BehaviorModel behavior) {
			if (lookAtPlayer.HasValue) {
				behavior.LookAtPlayer = lookAtPlayer.Value;
			}
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			lookAtPlayer = LookAtPlayer
		};
	}
}
