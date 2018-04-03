using System;
using System.IO;

public class FigureRecipeLoader {
	private readonly DsonObjectLocator objectLocator;

	public FigureRecipeLoader(DsonObjectLocator objectLocator) {
		this.objectLocator = objectLocator;
	}

	public FigureRecipe LoadFigureRecipe(string figureName, FigureRecipe parentRecipe) {
		var importProperties = ImportProperties.Load(figureName);

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
				importProperties.HdCorrectionInitialValue);
			
			Persistance.Save(figureRecipeFile, recipeToPersist);
		}

		Console.WriteLine($"Loading {figureName}...");
		FigureRecipe recipe = Persistance.Load<FigureRecipe>(figureRecipeFile);
		return recipe;
	}
}
