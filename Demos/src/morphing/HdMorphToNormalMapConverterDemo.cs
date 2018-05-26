using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Collections.Generic;

public class HdMorphToNormalMapConverterDemo : IDemoApp {
	public void Run() {
		LeakTracking.Setup();

		var device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug, FeatureLevel.Level_11_1);
		var shaderCache = new ShaderCache(device);
		var fileLocator = new ContentFileLocator();
		var objectLocator = new DsonObjectLocator(fileLocator);
		var contentPackConfs = ContentPackImportConfiguration.LoadAll(CommonPaths.ConfDir);
		var pathManager = ImporterPathManager.Make(contentPackConfs);
		var loader = new FigureRecipeLoader(fileLocator, objectLocator, pathManager);
		var figureRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		var figure = figureRecipe.Bake(fileLocator, null);
		var geometry = figure.Geometry;

		var ldChannelInputs = figure.ChannelSystem.MakeDefaultChannelInputs();
		figure.ChannelsByName["PBMNavel?value"].SetValue(ldChannelInputs, 1);
		figure.ChannelsByName["PBMNipples?value"].SetValue(ldChannelInputs, 1);
		figure.ChannelsByName["CTRLRune7?value"].SetValue(ldChannelInputs, 1);
		
		var hdChannelInputs = figure.ChannelSystem.MakeDefaultChannelInputs();
		figure.ChannelsByName["PBMNavel?value"].SetValue(hdChannelInputs, 1);
		figure.ChannelsByName["PBMNipples?value"].SetValue(hdChannelInputs, 1);
		figure.ChannelsByName["CTRLRune7?value"].SetValue(hdChannelInputs, 1);
		figure.ChannelsByName["CTRLRune7HD?value"].SetValue(hdChannelInputs, 1);

		var converter = new HdMorphToNormalMapConverter(device, shaderCache, figure);

		List<HashSet<int>> surfaceSubsets = new List<HashSet<int>>{
			new HashSet<int>{ 9, 13, 5, 6 },
			new HashSet<int>{ 12 },
			new HashSet<int>{ 11, 2 },
			new HashSet<int>{ 16, 1 },
		};

		var maps = converter.Convert(ldChannelInputs, hdChannelInputs, figure.UvSets["Rune 7"], surfaceSubsets);

		string character = "rune";

		maps[0].Save(CommonPaths.WorkDir.File(character + "-face.png"));
		maps[1].Save(CommonPaths.WorkDir.File(character + "-torso.png"));
		maps[2].Save(CommonPaths.WorkDir.File(character + "-legs.png"));
		maps[3].Save(CommonPaths.WorkDir.File(character + "-arms.png"));

		foreach (var map in maps) {
			map.Dispose();
		}

		shaderCache.Dispose();
		device.Dispose();

		LeakTracking.Finish();
	}
}
