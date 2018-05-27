using OpenSubdivFacade;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Linq;

public class HdMorphToNormalMapConverter {
	private const int ExtraRefinementLevels = 1;

	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly Figure figure;

	public HdMorphToNormalMapConverter(Device device, ShaderCache shaderCache, Figure figure) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.figure = figure;
	}
	
	public NormalMapRenderer MakeNormalMapRenderer(ChannelInputs ldChannelInputs, ChannelInputs hdChannelInputs, UvSet uvSet) {
		var ldChannelOutputs = figure.Evaluate(null, ldChannelInputs);
		var hdChannelOutputs = figure.Evaluate(null, hdChannelInputs);

		var ldControlPositions = figure.Geometry.VertexPositions.Select(p => p).ToArray();
		figure.Morpher.Apply(ldChannelOutputs, ldControlPositions);

		var hdControlPositions = figure.Geometry.VertexPositions.Select(p => p).ToArray();
		figure.Morpher.Apply(hdChannelOutputs, hdControlPositions);

		var activeHdMorphs = figure.Morpher.LoadActiveHdMorphs(hdChannelOutputs);

		int maxLevel = activeHdMorphs.Max(morph => morph.Morph.MaxLevel) + ExtraRefinementLevels;
		
		var controlTopology = new QuadTopology(figure.Geometry.VertexCount, figure.Geometry.Faces);

		var applier = HdMorphApplier.Make(controlTopology, hdControlPositions);

		var controlUvTopology = new QuadTopology(uvSet.Uvs.Length, uvSet.Faces);
		var controlUvs = uvSet.Uvs;

		var refinement = new Refinement(controlTopology, maxLevel);
		var uvRefinement = new Refinement(controlUvTopology, maxLevel, BoundaryInterpolation.EdgeAndCorner);

		var topology = controlTopology;
		var ldPositions = ldControlPositions;
		var hdPositions = hdControlPositions;

		var uvTopology = controlUvTopology;
		var uvs = controlUvs;
		var texturedLdPositions = ExtractTexturedPositions(topology, uvTopology, ldPositions);

		for (int levelIdx = 1; levelIdx <= maxLevel; ++levelIdx) {
			topology = refinement.GetTopology(levelIdx);
			ldPositions = refinement.Refine(levelIdx, ldPositions);
			hdPositions = refinement.Refine(levelIdx, hdPositions);
			
			foreach (var activeHdMorph in activeHdMorphs) {
				applier.Apply(activeHdMorph.Morph, activeHdMorph.Weight, levelIdx, topology, hdPositions);
			}

			uvTopology = uvRefinement.GetTopology(levelIdx);
			uvs = uvRefinement.Refine(levelIdx, uvs);
			texturedLdPositions = uvRefinement.Refine(levelIdx, texturedLdPositions);
		}

		var ldLimit = refinement.Limit(ldPositions);
		var hdLimit = refinement.Limit(hdPositions);
		var uvLimit = uvRefinement.Limit(uvs);
		var texturedLdLimit = uvRefinement.Limit(texturedLdPositions);

		int[] faceMap = refinement.GetFaceMap();
		
		refinement.Dispose();
		uvRefinement.Dispose();

		var hdNormals = CalculateNormals(hdLimit);
		var ldNormals = CalculateNormals(ldLimit);
		var ldTangents = CalculateTangents(uvLimit, texturedLdLimit);
		
		int[] controlSurfaceMap = figure.Geometry.SurfaceMap;
		int[] surfaceMap = faceMap
			.Select(controlFaceIdx => controlSurfaceMap[controlFaceIdx])
			.ToArray();

		var renderer = new NormalMapRenderer(device, shaderCache, hdNormals, ldNormals, topology.Faces, uvLimit.values, ldTangents, uvTopology.Faces, surfaceMap);
		return renderer;
	}

	private Vector3[] CalculateNormals(LimitValues<Vector3> limitPositions) {
		int count = limitPositions.values.Length;

		Vector3[] normals = new Vector3[count];
		for (int i = 0; i < count; ++i) {
			var tanS = limitPositions.tangents1[i];
			var tanT = limitPositions.tangents2[i];
			normals[i] = Vector3.Normalize(Vector3.Cross(tanS, tanT));
		}
		return normals;
	}

	private Vector3[] CalculateTangents(LimitValues<Vector2> limitUvs, LimitValues<Vector3> limitTexturedPositions) {
		int count = limitUvs.values.Length;

		if (limitTexturedPositions.values.Length != count) {
			throw new Exception("count mismatch");
		}

		Vector3[] tangents = new Vector3[count];
		for (int i = 0; i < count; ++i) {
			tangents[i] = TangentSpaceUtilities.CalculatePositionDu(
				limitUvs.tangents1[i], limitUvs.tangents2[i],
				limitTexturedPositions.tangents1[i], limitTexturedPositions.tangents2[i]);
		}
		return tangents;
	}
	
	public NormalMapRenderer MakeNormalMapRenderer(Figure figureWithGrafts, UvSet uvSetWithGrafts, ChannelInputs shapeInputsWithGrafts) {
		/*
		 * HUGE HACKS:
		 * This class only works on figures without grafts whereas everything else works on figures with grafts.
		 * So I have to convert the uvSet and shapeInputs the with-grafts figure to the without-grafts figure.
		 */

		var uvSet = figure.UvSets[uvSetWithGrafts.Name];

		ChannelInputs ldInputs = figure.MakeDefaultChannelInputs();
		ChannelInputs hdInputs = figure.MakeDefaultChannelInputs();

		string hdCorrectionMorphChannelName = HdCorrectionMorphSynthesizer.CalcChannelName(figure.Name);
		foreach (var channel in figure.Channels) {
			var channelWithGrafts = figureWithGrafts.ChannelsByName[channel.Name];

			double value = channelWithGrafts.GetInputValue(shapeInputsWithGrafts);
			channel.SetValue(ldInputs, value);
			if (channel.Name != hdCorrectionMorphChannelName) {
				channel.SetValue(hdInputs, value);
			}
		}
		
		return MakeNormalMapRenderer(ldInputs, hdInputs, uvSet);
	}

	private static Vector3[] ExtractTexturedPositions(QuadTopology topology, QuadTopology uvTopology, Vector3[] ldPositions) {
		int[] texturedToSpatialIndexMap = QuadTopology.CalculateVertexIndexMap(uvTopology, topology.Faces);
		return texturedToSpatialIndexMap
			.Select(spatialIdx => ldPositions[spatialIdx])
			.ToArray();
	}
}
