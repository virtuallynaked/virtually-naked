using SharpDX;

public class HdMorphApplier {
	public static HdMorphApplier Make(QuadTopology controlTopology, Vector3[] controlPositions) {
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

		return new HdMorphApplier(tangentToObjectSpaceTransforms);
	}
	
	private readonly Matrix3x3[] tangentToObjectSpaceTransforms;
	
	public HdMorphApplier(Matrix3x3[] tangentToObjectSpaceTransforms) {
		this.tangentToObjectSpaceTransforms = tangentToObjectSpaceTransforms;
	}

	public void Apply(HdMorph morph, float weight, int levelIdx, QuadTopology topology, Vector3[] positions) {
		var level = morph.GetLevel(levelIdx);
		if (level == null) {
			return;
		}

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

				positions[refinedVertexIdx] += weight * objectSpaceDelta;
			}
		}
	}
}
