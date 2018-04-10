using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class ScatteringFormFactorCalculator {
	public static ScatteringFormFactorCalculator Make(Geometry geometry) {
		int vertexCount = geometry.VertexCount;

		var refinementResult = geometry.AsTopology().Refine(0);
		Vector3[] vertexPositions = RefineVertexPositions(geometry, refinementResult);
		
		Quad[] faces = refinementResult.Mesh.Topology.Faces;
		int[] surfaceMap = geometry.SurfaceMap;

		return new ScatteringFormFactorCalculator(vertexPositions, faces, surfaceMap);
	}
	
	private static Vector3[] RefineVertexPositions(Geometry geometry, RefinementResult refinementResult) {
		var stencils = refinementResult.Mesh.Stencils.Map(stencil => new WeightedIndex(stencil.Index, stencil.Weight));
		Vector3[] refinedVertexPositions = new Subdivider(stencils).Refine(geometry.VertexPositions, new Vector3Operators());
		return refinedVertexPositions;
	}
		
	private Vector3[] vertexPositions;
	private Quad[] faces;
	private int[] surfaceMap;

	private int vertexCount;
	private CollisionTree receiverCollisionTree;
	private ConnectedComponentLabels connectedComponentLabels;
	
	private ScatteringFormFactorCalculator(Vector3[] vertexPositions, Quad[] faces, int[] surfaceMap) {
		if (faces.Length != surfaceMap.Length) {
			throw new ArgumentException("face count mismatch");
		}

		foreach (var face in faces) {
			if (face.IsDegeneratedIntoTriangle) {
				throw new NotImplementedException("ScatteringFormFactorCalculator only supports Quad faces");
			}
		}

		this.vertexPositions = vertexPositions;
		this.faces = faces;
		this.surfaceMap = surfaceMap;

		vertexCount = vertexPositions.Length;
		receiverCollisionTree = CollisionTree.Make(vertexPositions);
		connectedComponentLabels = ConnectedComponentLabels.Make(vertexCount, faces);
	}

	public PackedLists<WeightedIndex> Calculate(ScatteringProfile[] profilesBySurface) {
		double[][] rawFormFactors = CalculateRawFormFactors(profilesBySurface);
		var packedNormalizedFormFactors = NormalizeAndPackFormFactors(rawFormFactors);
		return packedNormalizedFormFactors;
	}
	
	/**
	 *  Returns form factors unnormalized by receiver
	 */
	private double[][] CalculateRawFormFactors(ScatteringProfile[] profilesBySurface) {
		double[][] formFactors = new double[vertexCount][]; //indexed by (receiver, transmitter)
		for (int receiverIdx = 0; receiverIdx < vertexCount; receiverIdx++) {
			formFactors[receiverIdx] = new double[vertexCount];
		}
		
		var stopwatch = Stopwatch.StartNew();

		int transmitterCount = faces.Length;
		for (int transmitterIdx = 0; transmitterIdx < transmitterCount; ++transmitterIdx) {
			if (transmitterIdx % 1000 == 0) {
				double fractionComplete = transmitterIdx / (double) transmitterCount;
				double estimatedRemainingSeconds = stopwatch.ElapsedMilliseconds / 1000.0 / fractionComplete;
				Console.WriteLine(fractionComplete + ": " + estimatedRemainingSeconds);
			}

			int transmitterLabel = connectedComponentLabels.FaceLabels[transmitterIdx];

			int surfaceIdx = surfaceMap[transmitterIdx];
			ScatteringProfile profile = profilesBySurface[surfaceIdx];
			if (profile == null) {
				//not a scattering surface
				continue;
			}
			
			Quad transmitter = faces[transmitterIdx];
			PositionedQuad positionedTransmitter = PositionedQuad.Make(vertexPositions, transmitter);
			double transmitterArea = positionedTransmitter.Area;
			
			if (profile.meanFreePath == 0) {
				//edge case: no scattering
				//split the face contribution evenly amongst its four corners
				double contribution = 0.25 * profile.surfaceAlbedo * transmitterArea;
				formFactors[transmitter.Index0][transmitter.Index0] = contribution;
				formFactors[transmitter.Index1][transmitter.Index1] = contribution;
				formFactors[transmitter.Index2][transmitter.Index2] = contribution;
				formFactors[transmitter.Index3][transmitter.Index3] = contribution;
				continue;
			}

			double imperceptibleDistance = profile.FindImperceptibleDistance(transmitterArea);
			var transmitterBoundingSphere = positionedTransmitter.BoundingSphere;
			var receiverBoundingSphere = new BoundingSphere(transmitterBoundingSphere.Center, (float) (transmitterBoundingSphere.Radius + imperceptibleDistance));
			List<int> receiverIndices = receiverCollisionTree.GetPointsInSphere(receiverBoundingSphere);

			Vector4[] contributions = receiverIndices
				.Select(receiverIdx => {
					int receiverLabel = connectedComponentLabels.VertexLabels[receiverIdx];
					if (receiverLabel != transmitterLabel) {
						return Vector4.Zero;
					}

					Vector3 receiverPosition = vertexPositions[receiverIdx];
					Vector4 contribution = profile.IntegrateOverQuad(receiverPosition, positionedTransmitter);
					return contribution;
				}).ToArray();

			double totalContribution = contributions.Sum(v => v.X + v.Y + v.Z + v.W);
			
			double normalizationFactor = profile.surfaceAlbedo * transmitterArea / totalContribution;
			
			for (int i = 0; i < contributions.Length; ++i) {
				int receiverIdx = receiverIndices[i];
				Vector4 contribution = contributions[i];

				formFactors[receiverIdx][transmitter.Index0] += normalizationFactor * contribution[0];
				formFactors[receiverIdx][transmitter.Index1] += normalizationFactor * contribution[1];
				formFactors[receiverIdx][transmitter.Index2] += normalizationFactor * contribution[2];
				formFactors[receiverIdx][transmitter.Index3] += normalizationFactor * contribution[3];
			}
		}

		return formFactors;
	}
	
	private PackedLists<WeightedIndex> NormalizeAndPackFormFactors(double[][] rawFormFactors) {
		List<List<WeightedIndex>> weightsByReceiver = new List<List<WeightedIndex>>();

		for (int receiverIdx = 0; receiverIdx < vertexCount; receiverIdx++) {
			double[] formFactorsForReceiver = rawFormFactors[receiverIdx];

			double total = 0;
			for (int transmitterIdx = 0; transmitterIdx < vertexCount; transmitterIdx++) {
				total += formFactorsForReceiver[transmitterIdx];
			}

			if (total == 0) {
				weightsByReceiver.Add(new List<WeightedIndex>());
				continue;
			}

			List<WeightedIndex> sortedWeights = Enumerable.Range(0, vertexCount)
				.Select(transmitterIdx => new WeightedIndex(transmitterIdx, (float) (formFactorsForReceiver[transmitterIdx] / total)))
				.Where(weightedIndex => weightedIndex.Weight != 0)
				.OrderByDescending(weightedIndex => weightedIndex.Weight)
				.ToList();

			List<WeightedIndex> topWeights = new List<WeightedIndex>();
			double accumulatedWeight = 0;
			foreach (WeightedIndex weightedIndex in sortedWeights) {
				topWeights.Add(weightedIndex);

				accumulatedWeight += weightedIndex.Weight;
				if (accumulatedWeight > 0.995) {
					break;
				}
			}

			if (topWeights.Count > 5000) {
				throw new InvalidOperationException("too many contributing vertices");
			}

			weightsByReceiver.Add(topWeights);
			
			if (receiverIdx % 1000 == 0) {
				Console.WriteLine(receiverIdx + ": " + topWeights.Count);
			}
		}

		var packedWeightsByReceiver = PackedLists<WeightedIndex>.Pack(weightsByReceiver);
		return packedWeightsByReceiver;
	}
}
