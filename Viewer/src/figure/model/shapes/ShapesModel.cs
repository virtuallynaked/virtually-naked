using System;
using System.Collections.Generic;

public class ShapesModel {
	public static ShapesModel Load(IArchiveDirectory figureDir, ChannelSystem channelSystem, string initialShapeName) {
		List<Shape> options = new List<Shape>();
		Shape activeShape = null;
		
		IArchiveDirectory shapesDirectory = figureDir.Subdirectory("shapes");
		if (shapesDirectory != null) {
			foreach (var shapeDirectory in shapesDirectory.Subdirectories) {
				var shape = Shape.Load(channelSystem, shapeDirectory);
				options.Add(shape);
				if (shape.Label == initialShapeName) {
					activeShape = shape;
				}
			}

			if (activeShape == null) {
				var message = string.Format("Initial shape '{0}' not found", initialShapeName);
				throw new Exception(message);
			}
		} else {
			var defaultShape = Shape.MakeDefault(channelSystem);
			options.Add(defaultShape);
			activeShape = defaultShape;
		}
		
		return new ShapesModel(options, activeShape);
	}

	public List<Shape> Options { get; }

	private Shape active;
	
	public event Action<Shape, Shape> ShapeChanged;
	
	public ShapesModel(List<Shape> options, Shape active) {
		Options = options;
		this.active = active;
	}
	
	public Shape Active {
		get {
			return active;
		}
		set {
			var old = active;
			active = value;
			ShapeChanged?.Invoke(old, value);
		}
	}

	public string ActiveName {
		get {
			return active.Label;
		}
		set {
			Active = Options.Find(option => option.Label == value);
		}
	}
}
