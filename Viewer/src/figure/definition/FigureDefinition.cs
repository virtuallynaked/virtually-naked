using System.Collections.Generic;

public class FigureDefinition {
	public static FigureDefinition Load(IArchiveDirectory dataDir, string name, FigureDefinition parent) {
		IArchiveDirectory figureDir = dataDir.Subdirectory("figures").Subdirectory(name);
		
		var channelSystemRecipe = Persistance.Load<ChannelSystemRecipe>(figureDir.File("channel-system-recipe.dat"));
		var channelSystem = channelSystemRecipe.Bake(parent?.ChannelSystem);
		
		BoneSystem boneSystem;
		if (parent != null) {
			boneSystem = parent.BoneSystem;
		} else {
			var boneSystemRecipe = Persistance.Load<BoneSystemRecipe>(figureDir.File("bone-system-recipe.dat"));
			boneSystem = boneSystemRecipe.Bake(channelSystem.ChannelsByName);
		}

		var shapeOptions = Shape.LoadAllForFigure(figureDir, channelSystem);
		var materialSetOptions = MaterialSetOption.LoadAllForFigure(figureDir);

		return new FigureDefinition(name, figureDir,
			channelSystem, boneSystem,
			shapeOptions, materialSetOptions);
	}
	
	public string Name { get; }
	public IArchiveDirectory Directory { get; }
	public ChannelSystem ChannelSystem { get; }
	public BoneSystem BoneSystem { get; }
	public List<Shape> ShapeOptions { get; }
	public List<MaterialSetOption> MaterialSetOptions { get; }

	public FigureDefinition(string name, IArchiveDirectory directory,
		ChannelSystem channelSystem, BoneSystem boneSystem,
		List<Shape> shapeOptions, List<MaterialSetOption> materialSetOptions) {
		Name = name;
		Directory = directory;
		ChannelSystem = channelSystem;
		BoneSystem = boneSystem;
		ShapeOptions = shapeOptions;
		MaterialSetOptions = materialSetOptions;
	}
}