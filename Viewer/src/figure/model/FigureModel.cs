using System;
using System.Collections.Generic;

public class FigureModel {
	public static FigureModel Load(IArchiveDirectory figureDir, string initialShapeName, string initialMaterialSetName, FigureModel parent) {
		FigureDefinition definition = FigureDefinition.Load(figureDir, parent?.definition);
		ShapesModel shapesModel = ShapesModel.Load(figureDir, definition.ChannelSystem, initialShapeName);
		MaterialsModel materialsModel = MaterialsModel.Load(figureDir, initialMaterialSetName);
		return new FigureModel(definition, shapesModel, materialsModel);
	}

	private readonly FigureDefinition definition;
	private readonly ShapesModel shapes;
	private readonly MaterialsModel materials;
	
	public FigureModel(FigureDefinition definition, ShapesModel shapes, MaterialsModel materials) {
		this.definition = definition;

		this.shapes = shapes;
		this.materials = materials;
	}
	
	public FigureDefinition Definition => definition;
	public ShapesModel Shapes => shapes;
	public MaterialsModel Materials => materials;
}
