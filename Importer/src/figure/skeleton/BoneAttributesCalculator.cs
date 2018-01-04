using SharpDX;
using System;
using System.Diagnostics;
using System.Linq;

public class BoneAttributesCalculator {
	private const float CubicCentimetersPerLiter = 1000;

	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly Geometry geometry;
	private readonly SkinBinding skinBinding;

	public BoneAttributesCalculator(ChannelSystem channelSystem, BoneSystem boneSystem, Geometry geometry, SkinBinding skinBinding) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;
		this.geometry = geometry;
		this.skinBinding = skinBinding;
	}

	private static float SignedTetrahedralVolume(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
		Vector3 p10 = p1 - p0;
		Vector3 p20 = p2 - p0;
		Vector3 p30 = p3 - p0;
		return 1/6f * Vector3.Dot(Vector3.Cross(p10, p20), p30);
	}

	private float[] CalculateBoneMasses() {
		Debug.Assert(skinBinding.BoneWeights.Count == geometry.VertexCount);

		float[] boneVolumes = new float[boneSystem.Bones.Count];

		foreach (var quad in geometry.Faces) {
			for (int cornerIdx = 0; cornerIdx < Quad.SideCount; ++cornerIdx) {
				int vertexIdx = quad.GetCorner(cornerIdx);
				foreach (var boneWeight in skinBinding.BoneWeights.GetElements(vertexIdx)) {
					var bone = skinBinding.Bones[boneWeight.Index];

					Vector3 p1 = geometry.VertexPositions[quad.GetCorner(cornerIdx - 1)];
					Vector3 p2 = geometry.VertexPositions[vertexIdx];
					Vector3 p3 = geometry.VertexPositions[quad.GetCorner(cornerIdx + 1)];

					var boneCenter = bone.CenterPoint.GetValue(channelSystem.DefaultOutputs);

					float volume = SignedTetrahedralVolume(boneCenter, p1, p2, p3) / CubicCentimetersPerLiter;
					boneVolumes[bone.Index] += 1/2f * volume; //half because each point is covered by two tetrahedra
				}
			}
		}

		float[] totalBoneVolumes = new float[boneSystem.Bones.Count];
		foreach (var bone in boneSystem.Bones) {
			for (var ancestor = bone; ancestor != null; ancestor = ancestor.Parent) {
				totalBoneVolumes[ancestor.Index] += boneVolumes[bone.Index];
			}
		}

		//assume a density of 1 kg per liter
		return totalBoneVolumes;
	}

	public BoneAttributes[] CalculateBoneAttributes() {
		float[] masses = CalculateBoneMasses();
		return masses
			.Select(mass => new BoneAttributes(mass))
			.ToArray();
	}
}