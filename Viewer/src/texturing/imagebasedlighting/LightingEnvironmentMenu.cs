using System;
using System.Collections.Generic;
using System.Linq;

public class SetLightingEnvironmentMenuItem : IToggleMenuItem {
	public static SetLightingEnvironmentMenuItem Make(ImageBasedLightingEnvironment environment, IArchiveDirectory environmentDir) {
		return new SetLightingEnvironmentMenuItem(environment, environmentDir.Name);
	}

	private readonly ImageBasedLightingEnvironment environment;
	private readonly string name;
	
	public SetLightingEnvironmentMenuItem(ImageBasedLightingEnvironment environment, string name) {
		this.environment = environment;
		this.name = name;
	}

	public string Label => name;

	public bool IsSet => environment.EnvironmentName == name;
	
	public void Toggle() {
		environment.EnvironmentName = name;
	}
}

public static class LightingEnvironmentMenu {
	private static double WrapRotation(double rotation) {
		while (rotation < -Math.PI) {
			rotation += 2 * Math.PI;
		}
		while (rotation > Math.PI) {
			rotation -= 2 * Math.PI;
		}
		return rotation;
	}

	private static IMenuItem MakeRotationItem(ImageBasedLightingEnvironment environment) {
		double epsilon = 1e-2;
		double max = Math.PI + epsilon;
		double min = -max;

		return new GenericRangeMenuItem("Rotation", min, 0, max,
			() => environment.Rotation,
			(value) => environment.Rotation = (float) WrapRotation(value));
	}

	private static List<IMenuItem> MakeEnvironmentsMenuItems(IArchiveDirectory environmentsDirectory, ImageBasedLightingEnvironment environment) {
		return environmentsDirectory.Subdirectories
			.Select(environmentDir => (IMenuItem) SetLightingEnvironmentMenuItem.Make(environment, environmentDir))
			.ToList();
	}

	public static IMenuLevel MakeMenuLevel(IArchiveDirectory dataDir, ImageBasedLightingEnvironment environment) {
		var environmentsDir = dataDir.Subdirectory("environments");

		List<IMenuItem> items = new List<IMenuItem>() {
			MakeRotationItem(environment)
		};
		items.AddRange(MakeEnvironmentsMenuItems(environmentsDir, environment));
		
		return new StaticMenuLevel(items.ToArray());
	}
}
