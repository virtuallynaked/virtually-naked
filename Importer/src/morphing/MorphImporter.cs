using SharpDX;
using System;
using System.Collections.Generic;

public class MorphImporter {
	private readonly List<MorphRecipe> morphRecipes = new List<MorphRecipe>();

	public MorphImporter() {
	}

	public IEnumerable<MorphRecipe> MorphRecipes => morphRecipes;

	public void ImportFrom(DsonTypes.Modifier modifier) {
		DsonTypes.Morph dsonMorph = modifier.morph;
		if (dsonMorph == null) {
			return;
		}

		float[][] dsonDeltas = dsonMorph.deltas?.values;
		if (dsonDeltas == null || dsonDeltas.Length == 0) {
			return;
		}
		
		if (modifier.channel.id != "value") {
			throw new InvalidOperationException("expected channel id to be 'value'");
		}
		string channel = modifier.name + "?value";
		
		int deltaCount = dsonDeltas.Length;
		MorphDelta[] deltas = new MorphDelta[deltaCount];
		for (int i = 0; i < deltaCount; ++i) {
			float[] dsonDelta = dsonDeltas[i];
			int vertexIdx = (int) dsonDelta[0];
            Vector3 positionOffset = new Vector3(dsonDelta[1], dsonDelta[2], dsonDelta[3]);
			deltas[i] = new MorphDelta(vertexIdx, positionOffset);
		}
		
		bool isHd = !String.IsNullOrEmpty(dsonMorph.hd_url);

		MorphRecipe recipe = new MorphRecipe {
			Channel = channel,
			Deltas = deltas
		};
		morphRecipes.Add(recipe);
	}

	public void ImportFrom(DsonTypes.Modifier[] modifiers) {
		if (modifiers == null) {
			return;
		}

		foreach (var modifier in modifiers) {
			ImportFrom(modifier);
		}
	}

	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.modifier_library);
	}

	public static IEnumerable<MorphRecipe> ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		MorphImporter importer = new MorphImporter();
		
		foreach (DsonTypes.DsonDocument doc in locator.GetAllDocumentsUnderPath(figureUris.MorphsBasePath)) {
			importer.ImportFrom(doc);
		}

		return importer.MorphRecipes;
	}
}
