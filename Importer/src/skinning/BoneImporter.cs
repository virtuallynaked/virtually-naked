using System.Collections.Generic;

public class BoneImporter {
	private readonly List<BoneRecipe> boneRecipes = new List<BoneRecipe>();

	public IEnumerable<BoneRecipe> BoneRecipes => boneRecipes;

	public void ImportFrom(DsonTypes.Node node) {
		BoneRecipe recipe = new BoneRecipe {
			Name = node.name,
			Parent = node.parent?.ReferencedObject.name,
			RotationOrder = node.rotation_order,
			InheritsScale = node.inherits_scale
		};
		boneRecipes.Add(recipe);
	}
	
	public void ImportFrom(DsonTypes.Node[] nodes) {
		if (nodes == null) {
			return;
		}

		foreach (var node in nodes) {
			ImportFrom(node);
		}
	}
	
	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.node_library);
	}

	public static IEnumerable<BoneRecipe> ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		BoneImporter importer = new BoneImporter();

		importer.ImportFrom(locator.LocateRoot(figureUris.DocumentUri));

		return importer.BoneRecipes;
	}
}
