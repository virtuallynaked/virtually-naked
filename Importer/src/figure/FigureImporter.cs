using System.Collections.Immutable;
using System.Linq;

class FigureImporter {
	public static FigureRecipe ImportFor(string figureName, DsonObjectLocator locator, FigureUris figureUris, FigureRecipe parentRecipe, double hdCorrectionInitialValue, ImmutableHashSet<string> visibleProducts) {
		var geometryRecipe = GeometryImporter.ImportForFigure(locator, figureUris);

		FigureRecipe recipe = new FigureRecipe {
			Name = figureName,
			Geometry = geometryRecipe,
			Channels = ChannelImporter.ImportForFigure(locator, figureUris, visibleProducts).ToList(),
			Formulas = FormulaImporter.ImportForFigure(locator, figureUris).ToList(),
			Bones = BoneImporter.ImportForFigure(locator, figureUris).ToList(),
			Morphs = MorphImporter.ImportForFigure(locator, figureUris).ToList(),
			SkinBinding = SkinBindingImporter.ImportForFigure(locator, figureUris),
			UvSets = UvSetImporter.ImportForFigure(locator, figureUris, geometryRecipe).ToList()
		};

		Geometry geometry = recipe.Geometry.Bake();

		var correctionSynthesizer = new HdCorrectionMorphSynthesizer(figureName, geometry);
		recipe.Channels.Add(correctionSynthesizer.SynthesizeChannel(hdCorrectionInitialValue));
		recipe.Morphs.Add(correctionSynthesizer.SynthesizeMorph());
		
		if (parentRecipe == null) {
			var scaleControlSynthesizer = new ScaleControlChannelSynthesizer(recipe.Bones);
			recipe.Channels.Add(scaleControlSynthesizer.SynthesizeChannel());
			recipe.Formulas.Add(scaleControlSynthesizer.SynthesizeFormula());
		}

		if (parentRecipe != null) {
			Geometry parentGeometry = parentRecipe.Geometry.Bake();
			recipe.Automorpher = AutomorpherRecipe.Make(parentGeometry, geometry);
		}
		
		return recipe;
	}
}
