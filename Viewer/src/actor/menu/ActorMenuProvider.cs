using System.Collections.Generic;

static class ActorMenuProvider {
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

	public static IMenuLevel MakeRootMenuLevel(Actor actor) {
		var parentModel = actor.Main.Model;
		var channelMenuLevel = ChannelMenuLevel.MakeRootLevelForFigure(parentModel);
		var poseControlsMenuLevel = channelMenuLevel.Extract(new string[] {"Pose Controls"});
		var expressionsMenuLevel = poseControlsMenuLevel.Extract(new string[] {"Head", "Expressions"});
		var behaviorMenuLevel = new BehaviorMenuLevel(parentModel.Behavior);

		var charactersMenuLevel = new CharactersMenuLevel(parentModel);

		var shapingItems = new List<IMenuItem>();
		var channelShapesMenuLevel = channelMenuLevel.Extract(new string[] {"Shapes"});
		shapingItems.Add(new ActionMenuItem("Reset Shape", () => parentModel.ResetShape()));
		shapingItems.AddRange(channelShapesMenuLevel.GetItems());
		var shapingMenuLevel = new StaticMenuLevel(shapingItems.ToArray());
		
		var animationsMenuLevel = new AnimationMenuLevel(parentModel.Animation);
		
		
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
		items.Add(new ActionMenuItem("Reset Pose", () => parentModel.ResetPose()));

		var figureMenuLevel = new StaticMenuLevel(items.ToArray());
		
		return figureMenuLevel;
	}
}
