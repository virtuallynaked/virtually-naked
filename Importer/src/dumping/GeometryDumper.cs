using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenSubdivFacade;

public class GeometryDumper {
	public static void DumpFigure(Figure figure) {
		var refinementDirectory = CommonPaths.WorkDir.Subdirectory("figures")
			.Subdirectory(figure.Name)
			.Subdirectory("refinement");
				
		GeometryDumper dumper = new GeometryDumper(figure, refinementDirectory);
		dumper.DumpRefinement();
	}
		
	private readonly Figure figure;
	private readonly DirectoryInfo refinementDirectory;

	private GeometryDumper(Figure figure, DirectoryInfo refinementDirectory) {
		this.figure = figure;
		this.refinementDirectory = refinementDirectory;
	}

	private void DumpControl() {
		var targetDirectory = refinementDirectory.Subdirectory("control");
		if (targetDirectory.Exists) {
			return;
		}

		targetDirectory.Create();
		targetDirectory.File("surface-map.array").WriteArray(figure.Geometry.SurfaceMap);
	}


	private void DumpRefinementLevel(int level) {
		var targetDirectory = refinementDirectory.Subdirectory("level-" + level);
		if (targetDirectory.Exists) {
			return;
		}

		Console.WriteLine("Dumping refined geometry level {0}...", level);

		MultisurfaceQuadTopology topology = figure.Geometry.AsTopology();
		
		var refinementResult = topology.Refine(level);
		
		targetDirectory.Create();
		Persistance.Save(targetDirectory.File("topology-info.dat"), refinementResult.TopologyInfo);
		SubdivisionMeshPersistance.Save(targetDirectory, refinementResult.Mesh);
		targetDirectory.File("control-face-map.array").WriteArray(refinementResult.ControlFaceMap);
	}
	
	private void DumpRefinement() {
		var surfaceProperties = SurfacePropertiesJson.Load(figure);

		DumpControl();

		DumpRefinementLevel(0);

		if (surfaceProperties.SubdivisionLevel != 0) {
			DumpRefinementLevel(surfaceProperties.SubdivisionLevel);
		}
    }
}
