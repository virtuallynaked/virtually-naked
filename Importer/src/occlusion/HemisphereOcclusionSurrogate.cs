using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class HemisphereOcclusionSurrogate {
	private static TriMesh SurrogateMesh = GeometricPrimitiveFactory.MakeOctahemisphere(4);

	private readonly Bone bone;
	private readonly List<int> attachedVertices;
	private readonly List<int> attachedFaces;

	public HemisphereOcclusionSurrogate(Bone bone, List<int> attachedVertices, List<int> attachedFaces) {
		this.bone = bone;
		this.attachedVertices = attachedVertices;
		this.attachedFaces = attachedFaces;
	}

	public int SurrogateVertexCount => SurrogateMesh.VertexCount;
	public List<int> AttachedFaces => attachedFaces;

	public BasicRefinedVertexInfo[] GetVertexInfos(ChannelOutputs outputs, Vector3[] controlPositions) {
		Vector3 center = bone.GetChainedTransform(outputs).Transform(bone.CenterPoint.GetValue(outputs));

		double totalDist = 0;
		foreach (int vertexIdx in attachedVertices) {
			totalDist += Vector3.Distance(controlPositions[vertexIdx], center);
		}
		double radius = totalDist / attachedVertices.Count;

		Quaternion rotation = bone.Parent.GetChainedTransform(outputs).RotationStage.Rotation;
		
		var transform = Matrix.AffineTransformation((float) radius * 1.2f, rotation, center);

		var transformedSurrogateMesh = SurrogateMesh.Transform(transform);

		return Enumerable.Range(0, transformedSurrogateMesh.VertexCount)
			.Select(idx => new BasicRefinedVertexInfo {
				position = transformedSurrogateMesh.VertexPositions[idx],
				normal = Vector3.Normalize(transformedSurrogateMesh.VertexNormals[idx])
			})
			.ToArray();
	}

	private static List<int> FindVerticesAttachedToBone(Figure figure, Bone bone) {
		var skinBinding = figure.SkinBinding;
		var boneIndex = skinBinding.Bones.IndexOf(bone);

		List<int> attachedVertices = new List<int>();

		for (int vertexIdx = 0; vertexIdx < figure.VertexCount; ++vertexIdx) {
			var boneWeights = skinBinding.BoneWeights.GetElements(vertexIdx);
			foreach (var boneWeight in boneWeights) {
				if (boneWeight.Index != boneIndex) {
					continue;
				}
				
				if (boneWeight.Weight != 1) {
					throw new Exception("must be fully attached or fully unattached");
				}

				attachedVertices.Add(vertexIdx);
			}
		}

		return attachedVertices;
	}

	private static List<int> FindAttachedFaces(Figure figure, List<int> attachedVertices) {
		List<int> attachedFaces = new List<int>();

		HashSet<int> attachedVerticesSet = new HashSet<int>(attachedVertices);

		for (int faceIdx = 0; faceIdx < figure.Geometry.Faces.Length; ++faceIdx) {
			Quad face = figure.Geometry.Faces[faceIdx];
			
			bool attached0 = attachedVerticesSet.Contains(face.Index0);
			bool attached1 = attachedVerticesSet.Contains(face.Index1);
			bool attached2 = attachedVerticesSet.Contains(face.Index2);
			bool attached3 = attachedVerticesSet.Contains(face.Index3);
			bool anyAttached = attached0 || attached1 || attached2 || attached3;

			if (anyAttached) {
				bool allAttached = attached0 && attached1 && attached2 && attached3;
				if (!allAttached) {
					throw new Exception("faces must be completely attached or completely unattached");
				}

				attachedFaces.Add(faceIdx);
			}
		}

		return attachedFaces;
	}

	public static HemisphereOcclusionSurrogate Make(Figure figure, Bone bone) {
		List<int> attachedVertices = FindVerticesAttachedToBone(figure, bone);
		List<int> attachedFaces = FindAttachedFaces(figure, attachedVertices);

		return new HemisphereOcclusionSurrogate(bone, attachedVertices, attachedFaces);
	}

	public static List<HemisphereOcclusionSurrogate> MakeForFigure(Figure figure) {
		if (figure.Name == "genesis-3-female") {
			return new List<HemisphereOcclusionSurrogate> {
				Make(figure, figure.BonesByName["lEye"]),
				Make(figure, figure.BonesByName["rEye"])
			};
		} else {
			return new List<HemisphereOcclusionSurrogate>();
		}
	}
}
