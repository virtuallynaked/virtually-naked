using SharpDX;
using System.Collections.Generic;
using System.Linq;

public class OcclusionSurrogate {
	public struct Info {
		Vector3 Center { get; }
		int Offset { get; }
		Quaternion Rotation;

		public Info(Vector3 center, int offset, Quaternion rotation) {
			Center = center;
			Offset = offset;
			Rotation = rotation;
		}
	}

	public static List<OcclusionSurrogate> MakeAll(FigureDefinition definition, OcclusionSurrogateParameters[] parametersList) {
		return parametersList.Select(parameters => Make(definition, parameters)).ToList();
	}

	public static OcclusionSurrogate Make(FigureDefinition definition, OcclusionSurrogateParameters parameters) {
		return new OcclusionSurrogate(definition.BoneSystem.Bones[parameters.BoneIndex], parameters.OffsetInOcclusionInfos);
	}

	private readonly Bone bone;
	private readonly int offsetInOcclusionInfos;

	public OcclusionSurrogate(Bone bone, int offsetInOcclusionInfos) {
		this.bone = bone;
		this.offsetInOcclusionInfos = offsetInOcclusionInfos;
	}

	public Info GetInfo(ChannelOutputs outputs) {
		return new Info(
			bone.CenterPoint.GetValue(outputs),
			offsetInOcclusionInfos,
			bone.GetRotation(outputs));
	}
}