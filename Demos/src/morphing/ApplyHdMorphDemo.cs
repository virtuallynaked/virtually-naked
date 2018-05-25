using OpenSubdivFacade;
using SharpDX;
using System;
using System.Collections.Immutable;

public class ApplyHdMorphDemo : IDemoApp {
	private readonly QuadTopology controlTopology;
	private readonly Vector3[] controlVertexPositions;

	public ApplyHdMorphDemo() {
		var fileLocator = new ContentFileLocator();
		var objectLocator = new DsonObjectLocator(fileLocator);
		var contentPackConfs = ContentPackImportConfiguration.LoadAll(CommonPaths.ConfDir);
		var pathManager = ImporterPathManager.Make(contentPackConfs);
		var loader = new FigureRecipeLoader(fileLocator, objectLocator, pathManager);
		var figureRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		var figure = figureRecipe.Bake(fileLocator, null);
		var geometry = figure.Geometry;
		controlTopology = new QuadTopology(geometry.VertexCount, geometry.Faces);
		controlVertexPositions = geometry.VertexPositions;
	}

	private void TestTopologyAssumptions() {
		using (var refinement = new Refinement(controlTopology, 2)) {
			var level1Topology = refinement.GetTopology(1);
			var level2Topology = refinement.GetTopology(2);
			HdMorphApplier.AssertTopologyAssumptions(controlTopology, level1Topology);
			HdMorphApplier.AssertTopologyAssumptions(level1Topology, level2Topology);
		}
	}

	private void TestHdMorph(HdMorph hdMorph, int refinedFaceIdxToCheck, int cornerToCheck, Vector3 expectedValue) {
		var (refinedTopology, refinedVertexPositions) = HdMorphApplier.ApplyHdMorph(hdMorph, controlTopology, controlVertexPositions);
		Vector3 actualValue = refinedVertexPositions[refinedTopology.Faces[refinedFaceIdxToCheck].GetCorner(cornerToCheck)];
		bool isOk = Vector3.Distance(actualValue, expectedValue) < 1e-2;
		Console.WriteLine($"{actualValue.FormatForMathematica()}: {(isOk ? "OK" : "FAIL")}");
	}

	private void TestLevel1HdMorph() {
		var delta = new Vector3(10, 20, 30);
		var vertexEdit = new HdMorph.VertexEdit(HdMorph.VertexEdit.PackPath(0, 2), delta);
		var faceEdit = new HdMorph.FaceEdit(100, ImmutableArray.Create(vertexEdit));

		var level1 = new HdMorph.Level(controlTopology.Faces.Length, 1, ImmutableArray.Create(faceEdit));
		var levels = ImmutableArray.Create(level1);
		HdMorph hdMorph = new HdMorph(levels);

		Vector3 expectedPosition = new Vector3(29.7680817f, 183.135971f, 13.2819338f); //from Daz studio export
		TestHdMorph(hdMorph, 100 * 4, 2, expectedPosition);
	}

	private void TestLevel2HdMorph() {
		var delta = new Vector3(10, 20, 30);
		var vertexEdit = new HdMorph.VertexEdit(HdMorph.VertexEdit.PackPath(0, 2, 1), delta);
		var faceEdit = new HdMorph.FaceEdit(200, ImmutableArray.Create(vertexEdit));

		var level1 = new HdMorph.Level(controlTopology.Faces.Length, 1, ImmutableArray<HdMorph.FaceEdit>.Empty);
		var level2 = new HdMorph.Level(controlTopology.Faces.Length, 2, ImmutableArray.Create(faceEdit));
		var levels = ImmutableArray.Create(level1, level2);
		HdMorph hdMorph = new HdMorph(levels);

		Vector3 expectedPosition = new Vector3(7.82922983f, 197.132507f, -15.0236731f); //from Daz studio export
		TestHdMorph(hdMorph, ((200 * 4) + 0) * 4 + 2, 1, expectedPosition);
	}

	public void Run() {
		TestTopologyAssumptions();
		TestLevel1HdMorph();
		TestLevel2HdMorph();
	}
}
