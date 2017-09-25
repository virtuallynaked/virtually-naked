using System.Collections.Generic;

static class FigureGroupMenuProvider {
	public static IMenuLevel MakeHairMenuLevel(FigureFacade hairFigure) {
		if (hairFigure == null) {
			return null;
		}

		var shapesMenuLevel = new ShapesMenuLevel(hairFigure.Model);
		var materialsMenuLevel = new MaterialsMenuLevel(hairFigure.Model.Materials);

		var hairMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Style", shapesMenuLevel),
			new SubLevelMenuItem("Color", materialsMenuLevel)
		);

		return hairMenuLevel;
	}

	public static IMenuLevel MakeRootMenuLevel(FigureGroup group) {
		var parentModel = group.Parent.Model;
		var channelMenuLevel = ChannelMenuLevel.MakeRootLevelForFigure(parentModel);
		var poseControlsMenuLevel = channelMenuLevel.Extract(new string[] {"Pose Controls"});
		var expressionsMenuLevel = poseControlsMenuLevel.Extract(new string[] {"Head", "Expressions"});
		var behaviourMenuLevel = new BehaviourMenuLevel(parentModel.Behaviour);

		var charactersMenuLevel = new CharactersMenuLevel(parentModel);

		var shapingItems = new List<IMenuItem>();
		var channelShapesMenuLevel = channelMenuLevel.Extract(new string[] {"Shapes"});
		shapingItems.Add(new ActionMenuItem("Reset Shape", () => parentModel.ResetShape()));
		shapingItems.AddRange(channelShapesMenuLevel.GetItems());
		var shapingMenuLevel = new StaticMenuLevel(shapingItems.ToArray());
		
		var animationsMenuLevel = new AnimationMenuLevel(parentModel.Animation);
		
		
		List<IMenuItem> items = new List<IMenuItem> { };
		items.Add(new SubLevelMenuItem("Characters", charactersMenuLevel));
		
		var hairMenuLevel = MakeHairMenuLevel(group.Hair);
		if (hairMenuLevel != null) {
			items.Add(new SubLevelMenuItem("Hair", hairMenuLevel));
		}

		items.Add(new SubLevelMenuItem("Shaping", shapingMenuLevel));
		items.Add(new SubLevelMenuItem("Behaviour", behaviourMenuLevel));
		items.Add(new SubLevelMenuItem("Expressions", expressionsMenuLevel));
		items.Add(new SubLevelMenuItem("Posing", poseControlsMenuLevel));
		items.Add(new SubLevelMenuItem("Animations", animationsMenuLevel));
		items.Add(new ActionMenuItem("Reset Pose", () => parentModel.ResetPose()));

		var figureMenuLevel = new StaticMenuLevel(items.ToArray());
		
		return figureMenuLevel;
	}
}
