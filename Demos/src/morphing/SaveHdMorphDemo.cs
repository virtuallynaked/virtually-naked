using SharpDX;
using System.Collections.Immutable;
using System.IO;

public class SaveHdMorphDemo : IDemoApp {
	private const int G3FControlFaceCount = 17000;
	
	public void Run() {
		var delta = new Vector3(10, 20, 30);
		var vertexEdit = new HdMorph.VertexEdit(HdMorph.VertexEdit.PackPath(0, 2, 1), delta);
		var faceEdit = new HdMorph.FaceEdit(200, ImmutableArray.Create(vertexEdit));

		var level1 = new HdMorph.Level(G3FControlFaceCount, 1, ImmutableArray<HdMorph.FaceEdit>.Empty);
		var level2 = new HdMorph.Level(G3FControlFaceCount, 2, ImmutableArray.Create(faceEdit));
		var levels = ImmutableArray.Create(level1, level2);
		HdMorph hdMorph = new HdMorph(levels);

		var outFile = new FileInfo(@"C:\Users\Public\Documents\My DAZ 3D Library\data\DAZ 3D\Genesis 3\Female\Morphs\GregTest\FBM-GregHDTest.dhdm");
		HdMorphSerialization.SaveHdMorph(outFile, hdMorph);
	}
}
