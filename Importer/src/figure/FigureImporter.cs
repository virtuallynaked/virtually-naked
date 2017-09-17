using System.Linq;

class FigureImporter {
	public static FigureRecipe ImportFor(DsonObjectLocator locator, FigureUris figureUris, FigureRecipe parentRecipe, double hdCorrectionInitialValue) {
		var geometryRecipe = GeometryImporter.ImportForFigure(locator, figureUris);

		FigureRecipe recipe = new FigureRecipe {
			Geometry = geometryRecipe,
			Channels = ChannelImporter.ImportForFigure(locator, figureUris).ToList(),
			Formulas = FormulaImporter.ImportForFigure(locator, figureUris).ToList(),
			Bones = BoneImporter.ImportForFigure(locator, figureUris).ToList(),
			Morphs = MorphImporter.ImportForFigure(locator, figureUris).ToList(),
			SkinBinding = SkinBindingImporter.ImportForFigure(locator, figureUris),
			UvSets = UvSetImporter.ImportForFigure(locator, figureUris, geometryRecipe).ToList()
		};

		Geometry geometry = recipe.Geometry.Bake();

		var correctionSynthesizer = new HdCorrectionMorphSynthesizer(figureUris.RootNodeId, geometry);
		recipe.Channels.Add(correctionSynthesizer.SynthesizeChannel(hdCorrectionInitialValue));
		recipe.Morphs.Add(correctionSynthesizer.SynthesizeMorph());
		
		if (parentRecipe != null) {
			Geometry parentGeometry = parentRecipe.Geometry.Bake();
			recipe.Automorpher = AutomorpherRecipe.Make(parentGeometry, geometry);
		}
		
		return recipe;
	}
}
