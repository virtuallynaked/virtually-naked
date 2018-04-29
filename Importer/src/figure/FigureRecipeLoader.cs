using System;

public class FigureRecipeLoader {
	private readonly DsonObjectLocator objectLocator;
	private readonly ImporterPathManager pathManager;

	public FigureRecipeLoader(DsonObjectLocator objectLocator, ImporterPathManager pathManager) {
		this.objectLocator = objectLocator;
		this.pathManager = pathManager;
	}

	public FigureRecipe LoadFigureRecipe(string figureName, FigureRecipe parentRecipe) {
		var importProperties = ImportProperties.Load(pathManager, figureName);

		var figureRecipesDirectory = CommonPaths.WorkDir.Subdirectory("recipes/figures");
		figureRecipesDirectory.Create();
		
		var figureRecipeFile = figureRecipesDirectory.File($"{figureName}.dat");
		if (!figureRecipeFile.Exists) {
			Console.WriteLine($"Reimporting {figureName}...");
			FigureRecipe recipeToPersist = FigureImporter.ImportFor(
				figureName,
				objectLocator,
				importProperties.Uris,
				parentRecipe,
				importProperties.HdCorrectionInitialValue,
				importProperties.VisibleProducts);
			
			Persistance.Save(figureRecipeFile, recipeToPersist);
		}

		Console.WriteLine($"Loading {figureName}...");
		FigureRecipe recipe = Persistance.Load<FigureRecipe>(figureRecipeFile);
		return recipe;
	}
}
