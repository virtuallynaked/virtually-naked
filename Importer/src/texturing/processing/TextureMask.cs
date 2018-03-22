using SharpDX;
using System;
using System.Collections.Generic;

public class TextureMask {
	public static TextureMask Make(UvSet uvSet, int[] surfaceMap, int surfaceIdx) {
		return new TextureMask(uvSet, surfaceMap, surfaceIdx);
	}
	
	private readonly UvSet uvSet;
	private readonly int[] surfaceMap;
	private readonly HashSet<int> surfaceIdxs = new HashSet<int>();
	
	public TextureMask(UvSet uvSet, int[] surfaceMap, int surfaceIdx) {
		this.uvSet = uvSet;
		this.surfaceMap = surfaceMap;
		surfaceIdxs.Add(surfaceIdx);
	}
	
	public List<int> GetMaskTriangleIndices() {
		List<int> triangleIndices = new List<int>();
		for (int faceIdx = 0; faceIdx < uvSet.Faces.Length; ++faceIdx) {
			int surfaceIdx = surfaceMap[faceIdx];
			if (!surfaceIdxs.Contains(surfaceIdx)) {
				continue;
			}

			Quad face = uvSet.Faces[faceIdx];
			triangleIndices.Add(face.Index0);
			triangleIndices.Add(face.Index1);
			triangleIndices.Add(face.Index2);
			
			triangleIndices.Add(face.Index2);
			triangleIndices.Add(face.Index3);
			triangleIndices.Add(face.Index0);
		}

		return triangleIndices;
	}

	public void Merge(TextureMask other) {
		if (uvSet != other.uvSet) {
			throw new InvalidOperationException("texture file conflict");
		}

		if (surfaceMap != other.surfaceMap) {
			throw new InvalidOperationException("texture type conflict");
		}

		surfaceIdxs.UnionWith(other.surfaceIdxs);
	}

	public Vector2[] GetMaskVertices() {
		return uvSet.Uvs;
	}
}
