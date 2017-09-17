using System;
using System.IO;

public class SystemDumper {
	private Figure figure;
	private bool[] channelsToInclude;
	private DirectoryInfo targetDirectory;

	public SystemDumper(Figure figure, bool[] channelsToInclude) {
		this.figure = figure;
		this.channelsToInclude = channelsToInclude;

		this.targetDirectory = CommonPaths.WorkDir.Subdirectory("figures")
			.Subdirectory(figure.Name);
	}

	private void Dump<T>(string filename, Func<T> factoryFunc) {
		var fileInfo = targetDirectory.File(filename);

		if (fileInfo.Exists) {
			return;
		}

		T obj = factoryFunc();
		
		targetDirectory.CreateWithParents();
		Persistance.Save(fileInfo, obj);
	}
	
	public void DumpAll() {
		var surfaceProperties = SurfacePropertiesJson.Load(figure);
		targetDirectory.CreateWithParents();
		Persistance.Save(targetDirectory.File("surface-properties.dat"), surfaceProperties);
		
		Dump("shaper-parameters.dat", () => figure.MakeShaperParameters(channelsToInclude));
		Dump("channel-system-recipe.dat", () => figure.MakeChannelSystemRecipe());

		if (figure.Parent == null) {
			Dump("bone-system-recipe.dat", () => figure.MakeBoneSystemRecipe());
			Dump("inverter-parameters.dat", () => figure.MakeInverterParameters());
		}
	}

	public static void DumpFigure(Figure figure, bool[] channelsToInclude) {
		new SystemDumper(figure, channelsToInclude).DumpAll();
	}
}
