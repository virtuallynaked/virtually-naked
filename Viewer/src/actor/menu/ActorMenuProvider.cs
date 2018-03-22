using System.Collections.Generic;

static class ActorMenuProvider {
	public static IMenuLevel MakeHairMenuLevel(FigureFacade hairFigure) {
		if (hairFigure == null) {
			return null;
		}

		var shapesMenuLevel = new ShapesMenuLevel(hairFigure.Definition, hairFigure.Model);
		var materialsMenuLevel = new MaterialsMenuLevel(hairFigure.Definition, hairFigure.Model);

		var hairMenuLevel = new StaticMenuLevel(
			new SubLevelMenuItem("Style", shapesMenuLevel),
			new SubLevelMenuItem("Color", materialsMenuLevel)
		);

		return hairMenuLevel;
	}

	public static IMenuLevel MakeRootMenuLevel(Actor actor) {
		var model = actor.Model;
		var channelMenuLevel = ChannelMenuLevel.MakeRootLevelForFigure(model);
		var poseControlsMenuLevel = channelMenuLevel.Extract(new string[] {"Pose Controls"});
		var expressionsMenuLevel = poseControlsMenuLevel.Extract(new string[] {"Head", "Expressions"});
		var behaviorMenuLevel = new BehaviorMenuLevel(model.Behavior);

		var charactersMenuLevel = new CharactersMenuLevel(actor.Main.Definition, actor.Main.Model);

		var shapingItems = new List<IMenuItem>();
		var channelShapesMenuLevel = channelMenuLevel.Extract(new string[] {"Shapes"});
		shapingItems.Add(new ActionMenuItem("Reset Shape", () => model.ResetShape()));
		shapingItems.AddRange(channelShapesMenuLevel.GetItems());
		var shapingMenuLevel = new StaticMenuLevel(shapingItems.ToArray());
		
		var animationsMenuLevel = new AnimationMenuLevel(model.Animation);
		
		List<IMenuItem> items = new List<IMenuItem> { };
		items.Add(new SubLevelMenuItem("Characters", charactersMenuLevel));
		
		var hairMenuLevel = MakeHairMenuLevel(actor.Hair);
		if (hairMenuLevel != null) {
			items.Add(new SubLevelMenuItem("Hair", hairMenuLevel));
		}

		items.Add(new SubLevelMenuItem("Shaping", shapingMenuLevel));
		items.Add(new SubLevelMenuItem("Behavior", behaviorMenuLevel));
		items.Add(new SubLevelMenuItem("Expressions", expressionsMenuLevel));
		items.Add(new SubLevelMenuItem("Posing", poseControlsMenuLevel));
		items.Add(new SubLevelMenuItem("Animations", animationsMenuLevel));
		items.Add(new ActionMenuItem("Reset Pose", () => model.ResetPose()));

		var figureMenuLevel = new StaticMenuLevel(items.ToArray());
		
		return figureMenuLevel;
	}
}
