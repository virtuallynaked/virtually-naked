using System;

public class CalculateBoneAttributesDemo : IDemoApp {
	public void Run() {
		var fileLocator = new ContentFileLocator();
		var objectLocator = new DsonObjectLocator(fileLocator);
		var contentPackConfs = ContentPackImportConfiguration.LoadAll(CommonPaths.ConfDir);
		var pathManager = ImporterPathManager.Make(contentPackConfs);
		var loader = new FigureRecipeLoader(fileLocator, objectLocator, pathManager);
		var figureRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		var figure = figureRecipe.Bake(fileLocator, null);

		var calculator = new BoneAttributesCalculator(figure.ChannelSystem, figure.BoneSystem, figure.Geometry, figure.SkinBinding);
		BoneAttributes[] boneAttributes = calculator.CalculateBoneAttributes();
		
		for (int boneIdx = 0; boneIdx < figure.Bones.Count; ++boneIdx) {
			Console.WriteLine("{0}: {1}", figure.Bones[boneIdx].Name, boneAttributes[boneIdx]);
		}
	}
}
