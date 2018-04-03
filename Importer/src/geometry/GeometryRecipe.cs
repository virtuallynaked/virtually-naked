using ProtoBuf;
using SharpDX;
using System.Collections.Generic;
using System;

public class GeometryRecipe {
	public GeometryType Type { get; }
	public Quad[] Faces { get; }
	public int[] FaceGroupMap { get; }
	public int[] SurfaceMap { get; }
	public Vector3[] VertexPositions { get; }
	public string[] FaceGroupNames { get; }
	public string[] SurfaceNames { get; }
	public string DefaultUvSet { get; }
	public Graft Graft { get; }

	public GeometryRecipe(GeometryType type, Quad[] faces, int[] faceGroupMap, int[] surfaceMap, Vector3[] vertexPositions, string[] faceGroupNames, string[] surfaceNames, string defaultUvSet, Graft graft) {
		Type = type;
		Faces = faces;
		VertexPositions = vertexPositions;
		FaceGroupMap = faceGroupMap;
		SurfaceMap = surfaceMap;
		FaceGroupNames = faceGroupNames;
		SurfaceNames = surfaceNames;
		DefaultUvSet = defaultUvSet;
		Graft = graft;
	}

	public Geometry Bake() {
		return new Geometry(Type, Faces, FaceGroupMap, SurfaceMap, VertexPositions, FaceGroupNames, SurfaceNames, Graft);
	}

	public int GetVertexCount() {
		return VertexPositions.Length;
	}

	public int GetSurfaceCount() {
		return SurfaceNames.Length;
	}
	
	public static GeometryRecipe Merge(FigureRecipeMerger.Reindexer reindexer, GeometryRecipe parent, GeometryRecipe[] children, AutomorpherRecipe[] childAutomorphers) {
		List<Quad> mergedFaces = new List<Quad>();
		List<int> mergedFaceGroupMap = new List<int>();
		List<int> mergedSurfaceMap = new List<int>();
		List<Vector3> mergedVertexPositions = new List<Vector3>();
		List<string> mergedSurfaceNames = new List<string>();
				
		for (int faceIdx = 0; faceIdx < parent.Faces.Length; faceIdx++) {
			if (reindexer.IsParentFaceHidden(faceIdx)) {
				continue;
			}
			
			mergedFaces.Add(parent.Faces[faceIdx]);
			mergedFaceGroupMap.Add(parent.FaceGroupMap[faceIdx]);
			mergedSurfaceMap.Add(parent.SurfaceMap[faceIdx]);
		}

		mergedVertexPositions.AddRange(parent.VertexPositions);
		mergedSurfaceNames.AddRange(parent.SurfaceNames);
		
		for (int childIdx = 0; childIdx < children.Length; ++childIdx) {
			GeometryRecipe child = children[childIdx];
			AutomorpherRecipe automorpher = childAutomorphers[childIdx];

			Dictionary<int, int> graftVertexMap = new Dictionary<int, int>();
			if (child.Graft != null) {
				foreach (var pair in child.Graft.VertexPairs) {
					graftVertexMap[pair.Source] = pair.Target;
				}
			}

			FigureRecipeMerger.Offset offset = reindexer.ChildOffsets[childIdx];

			if (child.Type != parent.Type) {
				throw new InvalidOperationException("children must have same geometry type as parent");
			}
			
			mergedSurfaceNames.AddRange(child.SurfaceNames);

			/*
			 * Children start "turned off" so instead of adding the child's base vertex positions here, I add the
			 * nearest positions on the parent's surface. Later I'll add a morph that moves the child vertices
			 * into place.
			 */
			mergedVertexPositions.AddRange(automorpher.ParentSurfacePositions);
			
			foreach (Quad face in child.Faces) {
				mergedFaces.Add(face.Map(idx => {
					if (graftVertexMap.TryGetValue(idx, out int graftIdx)) {
						return graftIdx;
					} else {
						return idx + offset.Vertex;
					}
				}));
			}

			int[] childToParentFaceGroupIdx = new int[child.FaceGroupNames.Length];
			for (int childFaceGroupIdx = 0; childFaceGroupIdx < child.FaceGroupNames.Length; ++childFaceGroupIdx) {
				string faceGroupName = child.FaceGroupNames[childFaceGroupIdx];
				int parentFaceGroupIdx = Array.FindIndex(parent.FaceGroupNames, name => name == faceGroupName);
				childToParentFaceGroupIdx[childFaceGroupIdx] = parentFaceGroupIdx;
			}

			foreach (int childFaceGroupIdx in child.FaceGroupMap) {
				int parentFaceGroupIdx = childToParentFaceGroupIdx[0];
				mergedFaceGroupMap.Add(parentFaceGroupIdx);
			}

			foreach (int surfaceIdx in child.SurfaceMap) {
				mergedSurfaceMap.Add(surfaceIdx + offset.Surface);
			}
		}
		
		return new GeometryRecipe(
			parent.Type,
			mergedFaces.ToArray(),
			mergedFaceGroupMap.ToArray(),
			mergedSurfaceMap.ToArray(),
			mergedVertexPositions.ToArray(),
			parent.FaceGroupNames,
			mergedSurfaceNames.ToArray(),
			parent.DefaultUvSet,
			null);
	}
}
