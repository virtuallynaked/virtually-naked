using System;
using System.Collections.Generic;
using System.Linq;

public class ConnectedComponentLabeller {
	private readonly int vertexCount;
	private readonly Quad[] faces;
	private readonly List<List<int>> adjacentFacesByVertex;

	private int labelCount;
	private int[] faceLabels;
	private int[] vertexLabels;

	public ConnectedComponentLabeller(int vertexCount, Quad[] faces) {
		this.vertexCount = vertexCount;
		this.faces = faces;
		adjacentFacesByVertex = Enumerable.Range(0, vertexCount)
			.Select(vertexIdx => new List<int>())
			.ToList();
	}

	public int LabelCount => labelCount;
	public int[] FaceLabels => faceLabels;
	public int[] VertexLabels => vertexLabels;

	public void Initialize() {
		FillAdjancencyTable();
		AssignFaceLabels();
		AssignedVertexLabels();
	}

	private void FillAdjancencyTable() {
		for (int faceIdx = 0; faceIdx < faces.Length; ++faceIdx) {
			Quad face = faces[faceIdx];
			for (int cornerIdx = 0; cornerIdx < Quad.SideCount; ++cornerIdx) {
				adjacentFacesByVertex[face.GetCorner(cornerIdx)].Add(faceIdx);
			}
		}
	}
	
	private void AssignFaceLabels() {
		labelCount = 0;

		faceLabels = new int[faces.Length];
		for (int faceIdx = 0; faceIdx < faces.Length; ++faceIdx) {
			faceLabels[faceIdx] = -1;
		}

		for (int faceIdx = 0; faceIdx < faces.Length; ++faceIdx) {
			if (faceLabels[faceIdx] == -1) {
				int label = labelCount;
				labelCount += 1;

				AssignLabelToConnectedFaces(label, faceIdx);
			}
		}
	}
	
	private void AssignLabelToConnectedFaces(int label, int faceIdx) {
		int currentLabel = faceLabels[faceIdx];
		if (currentLabel == label) {
			return;
		}

		if (currentLabel != -1) {
			throw new InvalidOperationException("a face cannot be assigned more than one label");
		}
		
		faceLabels[faceIdx] = label;
		Quad face = faces[faceIdx];
		for (int cornerIdx = 0; cornerIdx < Quad.SideCount; ++cornerIdx) {
			int vertexIdx = face.GetCorner(cornerIdx);
			foreach (int adjacentFaceIdx in adjacentFacesByVertex[vertexIdx]) {
				AssignLabelToConnectedFaces(label, adjacentFaceIdx);
			}
		}
	}
	
	private void AssignedVertexLabels() {
		vertexLabels = adjacentFacesByVertex
			.Select(adjacentFaces => {
				if (adjacentFaces.Count == 0) {
					return -1;
				} else {
					int label = faceLabels[adjacentFaces[0]];
					if (adjacentFaces.Any(faceIdx => faceLabels[faceIdx] != label)) {
						throw new InvalidOperationException("a vertex cannot be assigned more than one label");
					}
					return label;
				}
			})
			.ToArray();
	}
}
