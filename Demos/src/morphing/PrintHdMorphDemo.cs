using System;
using System.IO;

public class PrintHdMorphDemo : IDemoApp {
	private const int G3FControlFaceCount = 17000;
	private const int MaxFaceEditsToPrint = 2;
	
	private void ValidateHdMorph(HdMorph hdMorph) {
		hdMorph.Validate(G3FControlFaceCount);
		
		foreach (var level in hdMorph.Levels) {
			foreach (var faceEdit in level.FaceEdits) {
				foreach (var vertexEdit in faceEdit.VertexEdits) {
					float deltaLength = vertexEdit.Delta.Length();
					if (deltaLength > 1 || deltaLength < 1e-5) {
						throw new InvalidOperationException("found unusual delta: " + deltaLength);
					}
				}
			}
		}
	}

	private void PrintHdMorph(HdMorph hdMorph) {
		foreach (var level in hdMorph.Levels) {
			Console.WriteLine($"level {level.LevelIdx}:");

			for (int faceEditIdx = 0; faceEditIdx < level.FaceEdits.Length && faceEditIdx < MaxFaceEditsToPrint; ++faceEditIdx) {
				var faceEdit = level.FaceEdits[faceEditIdx];
				Console.WriteLine($"  face edit {faceEditIdx} of {level.FaceEdits.Length}:");
				Console.WriteLine($"    control-face-idx = {faceEdit.ControlFaceIdx}");
				
				foreach (var vertexEdit in faceEdit.VertexEdits) {
					string pathStr = string.Join(",", vertexEdit.Path);
					Console.WriteLine($"    [{pathStr}]: {vertexEdit.Delta.Length()} {vertexEdit.Delta.FormatForMathematica()}");
				}
			}
		}
	}

	public void Run() {
		//FileInfo file = CommonPaths.SourceAssetsDir.File("daz-assets/FWSAAdalineHDforVictoria7/Content/data/DAZ 3D/Genesis 3/Female/Morphs/FWSA/Adaline/FBM-FWSAAdaline.dhdm");
		//FileInfo file = CommonPaths.SourceAssetsDir.File("daz-assets/FWSAAdalineHDforVictoria7/Content/data/DAZ 3D/Genesis 3/Female/Morphs/FWArt/FW_PBMNails.dhdm");
		FileInfo file = CommonPaths.SourceAssetsDir.File("daz-assets/Rune7HDAddOn/Content/data/DAZ 3D/Genesis 3/Female/Morphs/Daz 3D/Rune 7/FBMRune7HD.dhdm");

		HdMorph hdMorph = HdMorphSerialization.LoadHdMorph(file);

		ValidateHdMorph(hdMorph);
		PrintHdMorph(hdMorph);
	}
}
