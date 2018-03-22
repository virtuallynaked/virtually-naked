using ProtoBuf;
using System.Collections.Generic;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class BoneRecipe {
	public string Name { get; set; }
	public string Parent { get; set; }
	public string RotationOrder { get; set; }
	public bool InheritsScale { get; set; }

	public void Bake(Dictionary<string, Channel> channels, List<Bone> bones, Dictionary<string, Bone> bonesByName) {
		int index = bones.Count;
		var centerPoint = ChannelTriplet.Lookup(channels, Name + "?center_point");
		var endPoint = ChannelTriplet.Lookup(channels, Name + "?end_point");
		var orientation = ChannelTriplet.Lookup(channels, Name + "?orientation");
		var rotation = ChannelTriplet.Lookup(channels, Name + "?rotation");
		var translation = ChannelTriplet.Lookup(channels, Name + "?translation");
		var scale = ChannelTriplet.Lookup(channels, Name + "?scale");
		var generalScale = channels[Name + "?scale/general"];

		Bone parent = Parent != null ? bonesByName[Parent] : null;
		Bone bone = new Bone(
			Name,
			index,
			parent,
			global::RotationOrder.FromString(RotationOrder),
			InheritsScale,
			centerPoint,
			endPoint,
			orientation,
			rotation,
			translation,
			scale,
			generalScale);

		bones.Add(bone);
		bonesByName.Add(Name, bone);
	}
}
