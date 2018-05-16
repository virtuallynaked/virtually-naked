using OpenSubdivFacade;
using SharpDX;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static SharpDX.Vector3;

public class UVSetDumper {
	public static void DumpFigure(Figure figure, SurfaceProperties surfaceProperties, DirectoryInfo figureDestDir) {
		DirectoryInfo uvSetsDirectory = figureDestDir.Subdirectory("uv-sets");
		UVSetDumper dumper = new UVSetDumper(figure, surfaceProperties, uvSetsDirectory);
		dumper.DumpShared();
		foreach (var pair in figure.UvSets) {
			dumper.Dump(pair.Key, pair.Value);
		}
	}
	
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly DirectoryInfo uvSetsDirectory;

	public UVSetDumper(
		Figure figure,
		SurfaceProperties surfaceProperties,
		DirectoryInfo uvSetsDirectory) {
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
		this.uvSetsDirectory = uvSetsDirectory;
	}
	
	public void DumpShared() {
		var texturedFacesFile = uvSetsDirectory.File("textured-faces.array");
		var textureToSpatialIdxMapFile = uvSetsDirectory.File("textured-to-spatial-idx-map.array");
		if (texturedFacesFile.Exists && textureToSpatialIdxMapFile.Exists) {
			return;
		}
		
		int subdivisionLevel = surfaceProperties.SubdivisionLevel;

		var geometry = figure.Geometry;
		var spatialControlTopology = new QuadTopology(geometry.VertexCount, geometry.Faces);
		QuadTopology spatialTopology;
		using (var refinement = new Refinement(spatialControlTopology, subdivisionLevel)) {
			spatialTopology = refinement.GetTopology();
		}

		var uvSet = figure.DefaultUvSet;
		var texturedControlTopology = new QuadTopology(uvSet.Uvs.Length, uvSet.Faces);
		QuadTopology texturedTopology;
		using (var refinement = new Refinement(texturedControlTopology, surfaceProperties.SubdivisionLevel, BoundaryInterpolation.EdgeAndCorner)) {
			texturedTopology = refinement.GetTopology();
		}

		int[] texturedToSpatialIndexMap = CalculateVertexIndexMap(texturedTopology, spatialTopology.Faces);

		uvSetsDirectory.CreateWithParents();
		texturedFacesFile.WriteArray(texturedTopology.Faces);
		textureToSpatialIdxMapFile.WriteArray(texturedToSpatialIndexMap);
	}
		
	public static int[] CalculateVertexIndexMap(QuadTopology sourceTopology, Quad[] destFaces) {
		int faceCount = sourceTopology.Faces.Length;
		if (destFaces.Length != faceCount) {
			throw new InvalidOperationException("face count mismatch");
		}

		int[] indexMap = new int[sourceTopology.VertexCount];
		for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
			Quad sourceFace = sourceTopology.Faces[faceIdx];
			Quad destFace = destFaces[faceIdx];
			
			for (int i = 0; i < Quad.SideCount; ++i) {
				int sourceIdx = sourceFace.GetCorner(i);
				int destIdx = destFace.GetCorner(i);

				int previousMapping = indexMap[sourceIdx];
				if (previousMapping != default(int) && previousMapping != destIdx) {
					throw new InvalidOperationException("mapping conflict");
				}

				indexMap[sourceIdx] = destIdx;
			}
		}

		return indexMap;
	}

	private UvSet RemapToDefault(UvSet uvSet) {
		var defaultUvSet = figure.DefaultUvSet;
		var defaultUvSetTopology = new QuadTopology(defaultUvSet.Uvs.Length, defaultUvSet.Faces);
		var indexMap = CalculateVertexIndexMap(defaultUvSetTopology, uvSet.Faces);

		Vector2[] remappedUvs = indexMap
			.Select(idx => uvSet.Uvs[idx])
			.ToArray();

		return new UvSet(uvSet.Name, remappedUvs, defaultUvSet.Faces);
	}

		
	public void Dump(string name, UvSet uvSet) {
		DirectoryInfo uvSetDirectory = uvSetsDirectory.Subdirectory(name);
		if (uvSetDirectory.Exists) {
			return;
		}

		Console.WriteLine($"Dumping uv-set {name}...");
		
		int subdivisionLevel = surfaceProperties.SubdivisionLevel;

		var geometry = figure.Geometry;
		var spatialControlTopology = new QuadTopology(geometry.VertexCount, geometry.Faces);
		var spatialControlPositions = geometry.VertexPositions;

		QuadTopology spatialTopology;
		LimitValues<Vector3> spatialLimitPositions;
		using (var refinement = new Refinement(spatialControlTopology, subdivisionLevel)) {
			spatialTopology = refinement.GetTopology();
			spatialLimitPositions = refinement.LimitFully(spatialControlPositions);
		}
		
		uvSet = RemapToDefault(uvSet);

		var texturedControlTopology = new QuadTopology(uvSet.Uvs.Length, uvSet.Faces);
		Vector2[] controlTextureCoords = uvSet.Uvs;

		int[] controlSpatialIdxMap = CalculateVertexIndexMap(texturedControlTopology, spatialControlTopology.Faces);
		Vector3[] texturedControlPositions = controlSpatialIdxMap
			.Select(spatialIdx => spatialControlPositions[spatialIdx])
			.ToArray();

		QuadTopology texturedTopology;
		LimitValues<Vector3> texturedLimitPositions;
		LimitValues<Vector2> limitTextureCoords;
		using (var refinement = new Refinement(texturedControlTopology, surfaceProperties.SubdivisionLevel, BoundaryInterpolation.EdgeAndCorner)) {
			texturedTopology = refinement.GetTopology();
			texturedLimitPositions = refinement.LimitFully(texturedControlPositions);
			limitTextureCoords = refinement.LimitFully(controlTextureCoords);
		}

		Vector2[] textureCoords;
		if (geometry.Type == GeometryType.SubdivisionSurface) {
			textureCoords = limitTextureCoords.values;
		} else {
			if (subdivisionLevel != 0) {
				throw new InvalidOperationException("polygon meshes cannot be subdivided");
			}
			Debug.Assert(limitTextureCoords.values.Length == controlTextureCoords.Length);

			textureCoords = controlTextureCoords;
		}
				
		int[] spatialIdxMap = CalculateVertexIndexMap(texturedTopology, spatialTopology.Faces);

		TexturedVertexInfo[] texturedVertexInfos = Enumerable.Range(0, textureCoords.Length)
			.Select(idx => {
				int spatialVertexIdx = spatialIdxMap[idx];
				Vector2 textureCoord = textureCoords[idx];

				Vector3 positionDu = CalculatePositionDu(
					limitTextureCoords.tangents1[idx],
					limitTextureCoords.tangents2[idx],
					texturedLimitPositions.tangents1[idx],
					texturedLimitPositions.tangents2[idx]);
				
				Vector3 spatialPositionTan1 = spatialLimitPositions.tangents1[spatialVertexIdx];
				Vector3 spatialPositionTan2 = spatialLimitPositions.tangents2[spatialVertexIdx];
				
				Vector2 tangentUCoeffs = CalculateTangentSpaceRemappingCoeffs(spatialPositionTan1, spatialPositionTan2, positionDu);
				
				DebugUtilities.AssertFinite(tangentUCoeffs.X);
				DebugUtilities.AssertFinite(tangentUCoeffs.Y);

				return new TexturedVertexInfo(
					textureCoord,
					tangentUCoeffs);
			})
			.ToArray();

		uvSetDirectory.CreateWithParents();
		uvSetDirectory.File("textured-vertex-infos.array").WriteArray(texturedVertexInfos);
	}

	private float NormalizationFactor(float lengthSquared) {
		return lengthSquared == 0 ? 1 : (float) (1 / Math.Sqrt(lengthSquared));
	}

	private void Normalize(ref float x, ref float y) {
		float m = NormalizationFactor(x * x + y * y);
		x *= m;
		y *= m;
	}

	/**
	 * Calculate the derivative of position with respect to U given the derivatives of UV and position with
	 * respect to a shared coordinate system (S,T).
	 */
	private Vector3 CalculatePositionDu(Vector2 dUVdS, Vector2 dUVdT, Vector3 dPdS, Vector3 dPdT) {
		/*
		Derivation in Mathematica:
			p[s_, t_] := p0 + ps*s + pt*t
			u[s_, t_] := u0 + us*s + ut*t
			v[s_, t_] := v0 + vs*s + vt*t
			soln = Solve[u[s, t] == U && v[s, t] == V, {s, t}]
			P = p[s, t] /. soln
			D[P, U] // FullSimplify
			> {(pt*vs - ps*vt)/(ut*vs - us*vt)}
		*/
		
		float us = dUVdS.X;
		float vs = dUVdS.Y;
		float ut = dUVdT.X;
		float vt = dUVdT.Y;
		Normalize(ref us, ref ut);
		Normalize(ref vs, ref vt);
		
		Vector3 ps = dPdS;
		Vector3 pt = dPdT;

		float denom = (ut*vs - us*vt);
		if (denom == 0) {
			return Vector3.Zero;
		}
		Vector3 dPdU = Vector3.Normalize((pt*vs - ps*vt) / denom);
		return dPdU;
	}
	
	/**
	 * Given two vectors (from1, from2) forming a tangent-plane find a pair of coefficients (a,b) such
	 * that a * from1 + b * from2 == to.
	 * 
	 * Note that the system is overdetermined and 'to' might not lie exactly in a plane. In this case, the least
	 * squares solution is returned.
	 */
	private Vector2 CalculateTangentSpaceRemappingCoeffs(Vector3 from1, Vector3 from2, Vector3 to) {
		float normalizationFactor1 = NormalizationFactor(from1.LengthSquared());
		float normalizationFactor2 = NormalizationFactor(from2.LengthSquared());

		from1 = from1 * normalizationFactor1;
		from2 = from2 * normalizationFactor2;

		float a11 = Dot(from1, from1);
		float a12 = Dot(from1, from2);
		float a21 = Dot(from2, from1);
		float a22 = Dot(from2, from2);

		float b1 = Dot(from1, to);
		float b2 = Dot(from2, to);

		float det = a11 * a22 - a12 * a21;
		if (MathUtil.IsZero(det)) {
			return Vector2.Zero;
		}

		float m1 = (a22 * b1 - a12 * b2) * normalizationFactor1;
		float m2 = (a11 * b2 - a21 * b1) * normalizationFactor2;
		Normalize(ref m1, ref m2);

		return new Vector2(m1, m2);
	}
}
