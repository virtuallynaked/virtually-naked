using System;
using System.IO;

public class GeometryDumper {
	public static void DumpFigure(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo figureDestDir) {
		var refinementDirectory = figureDestDir.Subdirectory("refinement");
				
		GeometryDumper dumper = new GeometryDumper(figure, surfaceProperties, refinementDirectory);
		dumper.DumpRefinement();
	}

	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly DirectoryInfo refinementDirectory;

	private GeometryDumper(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo refinementDirectory) {
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
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
		DumpControl();

		DumpRefinementLevel(0);

		if (surfaceProperties.SubdivisionLevel != 0) {
			DumpRefinementLevel(surfaceProperties.SubdivisionLevel);
		}
    }
}
