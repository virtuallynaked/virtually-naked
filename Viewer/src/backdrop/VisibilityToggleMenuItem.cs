public class FloorVisibilityToggleMenuItem : IToggleMenuItem {
	private readonly PlayspaceFloor floor;

	public FloorVisibilityToggleMenuItem(PlayspaceFloor floor) {
		this.floor = floor;
	}

	public bool IsSet => floor.IsVisible;

	public string Label => "Floor";

	public void Toggle() {
		floor.IsVisible = !floor.IsVisible;
	}
}
