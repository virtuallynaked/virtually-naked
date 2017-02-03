using OpenSubdivFacade;
using SharpDX;
using System.Diagnostics;

public static class HdMorphApplier {
	[Conditional("DEBUG")]
	public static void AssertTopologyAssumptions(QuadTopology topology, QuadTopology nextLevelTopology) {
		for (int faceIdx = 0; faceIdx < topology.Faces.Length; ++faceIdx) {
			for (int cornerIdx = 0; cornerIdx < Quad.SideCount; ++cornerIdx) {
				int vertexIdx = topology.Faces[faceIdx].GetCorner(cornerIdx);
				int nextLevelVertexIdx = nextLevelTopology.Faces[faceIdx * 4 + cornerIdx].GetCorner(cornerIdx);
				Debug.Assert(vertexIdx == nextLevelVertexIdx);
			}
		}
	}

	private static (QuadTopology, Vector3[]) ApplyHdMorph(HdMorph hdMorph, QuadTopology controlTopology, Vector3[] controlPositions, Refinement refinement) {
		Matrix3x3[] tangentToObjectSpaceTransforms = new Matrix3x3[controlTopology.Faces.Length * 4];
		for (int faceIdx = 0; faceIdx < controlTopology.Faces.Length; ++faceIdx) {
			for (int cornerIdx = 0; cornerIdx < Quad.SideCount; ++cornerIdx) {
				Vector3 cur = controlPositions[controlTopology.Faces[faceIdx].GetCorner(cornerIdx)];
				Vector3 prev = controlPositions[controlTopology.Faces[faceIdx].GetCorner(cornerIdx - 1)];
				Vector3 next = controlPositions[controlTopology.Faces[faceIdx].GetCorner(cornerIdx + 1)];

				Vector3 edge1 = (prev - cur);
				Vector3 edge2 = (cur - next);

				Vector3 tangent = Vector3.Normalize(edge1);
				Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
				Vector3 bitangent = Vector3.Cross(tangent, normal);

				Matrix3x3 tangentToObjectSpaceTransform = new Matrix3x3(
					tangent.X, tangent.Y, tangent.Z,
					normal.X, normal.Y, normal.Z,
					bitangent.X, bitangent.Y, bitangent.Z);
				tangentToObjectSpaceTransforms[faceIdx * 4 + cornerIdx] = tangentToObjectSpaceTransform;
			}
		}

		var topology = controlTopology;
		var positions = controlPositions;

		foreach (var level in hdMorph.Levels) {
			topology = refinement.GetTopology(level.LevelIdx);
			positions = refinement.Refine(level.LevelIdx, positions);
			
			foreach (var faceEdit in level.FaceEdits) {
				foreach (var vertexEdit in faceEdit.VertexEdits) {
					int pathLength = vertexEdit.PathLength;
					int refinedFaceIdx = faceEdit.ControlFaceIdx;
					for (int i = 0; i < pathLength - 1; ++i) {
						refinedFaceIdx = refinedFaceIdx * 4 + vertexEdit.GetPathElement(i);
					}
					int cornerIdx = vertexEdit.GetPathElement(pathLength - 1);
					int refinedVertexIdx = topology.Faces[refinedFaceIdx].GetCorner(cornerIdx);

					Vector3 tangentSpaceDelta = vertexEdit.Delta;
					int tangentToObjectSpaceTransformIdx = faceEdit.ControlFaceIdx * 4 + vertexEdit.GetPathElement(0);
					Matrix3x3 tangentToObjectSpaceTransform = tangentToObjectSpaceTransforms[tangentToObjectSpaceTransformIdx];
					Vector3 objectSpaceDelta = Vector3.Transform(tangentSpaceDelta, tangentToObjectSpaceTransform);

					positions[refinedVertexIdx] += objectSpaceDelta;
				}
			}
		}

		return (topology, positions);
	}

	public static (QuadTopology, Vector3[]) ApplyHdMorph(HdMorph hdMorph, QuadTopology controlTopology, Vector3[] controlPositions) {
		using (var refinement = new Refinement(controlTopology, hdMorph.MaxLevel)) {
			return ApplyHdMorph(hdMorph, controlTopology, controlPositions, refinement);
		}
	}
}
