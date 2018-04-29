using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System.Collections.Generic;

class BoneSystemBuilder {
	private List<Channel> channels = new List<Channel>();
	private List<Bone> bones = new List<Bone>();

	private Channel AddChannel(string name, double initialValue) {
		var channel = new Channel(name, channels.Count, null, initialValue, 0, 0, false, true, false, name);
		channels.Add(channel);
		return channel;
	}

	private ChannelTriplet AddChannelTriplet(string name, Vector3 initialValue) {
		var xChannel = AddChannel(name + "?x", initialValue.X);
		var yChannel = AddChannel(name + "?y", initialValue.Y);
		var zChannel = AddChannel(name + "?z", initialValue.Z);
		return new ChannelTriplet(xChannel, yChannel, zChannel);
	}

	public Bone AddBone(string name, Bone parent, Vector3 centerPoint, Vector3 endPoint, Vector3 orientation) {
		var bone = new Bone(
			name, bones.Count, parent, RotationOrder.XYZ, false,
			AddChannelTriplet(name + "/center", centerPoint),
			AddChannelTriplet(name + "/end-point", endPoint),
			AddChannelTriplet(name + "/orientation", orientation),
			AddChannelTriplet(name + "/rotation", Vector3.Zero),
			AddChannelTriplet(name + "/translation", Vector3.Zero),
			AddChannelTriplet(name + "/scale", Vector3.One),
			AddChannel(name + "/general_scale", 1));
		bones.Add(bone);
		return bone;
	}

	public ChannelSystem BuildChannelSystem() {
		return new ChannelSystem(null, channels);
	}

	public BoneSystem BuildBoneSystem() {
		return new BoneSystem(bones);
	}
}

[TestClass]
public class RigidBoneSystemTest {
	[TestMethod]
	public void TestTransformConsistency() {
		var builder = new BoneSystemBuilder();
		var bone0 = builder.AddBone("bone0", null, new Vector3(1, 0, 0), new Vector3(2, 0, 0), Vector3.Zero);
		var bone1 = builder.AddBone("bone1", bone0, new Vector3(2, 0, 0), new Vector3(3, 0, 0), Vector3.Zero);
		var bone2 = builder.AddBone("bone2", bone1, new Vector3(3, 0, 0), new Vector3(4, 0, 0), Vector3.Zero);
		var channelSystem = builder.BuildChannelSystem();
		var boneSystem = builder.BuildBoneSystem();
		
		var rigidBoneSystem = new RigidBoneSystem(boneSystem);
		
		var baseInputs = channelSystem.MakeDefaultChannelInputs();
		bone1.Scale.SetValue(baseInputs, new Vector3(2, 3, 4));
		bone1.Translation.SetValue(baseInputs, new Vector3(4, 5, 6));
		bone2.Translation.SetValue(baseInputs, new Vector3(5, 6, 7));
		
		var baseOutputs = channelSystem.Evaluate(null, baseInputs);
		rigidBoneSystem.Synchronize(baseOutputs);
		var rigidBaseInputs = rigidBoneSystem.ReadInputs(baseOutputs);

		var rigidInputs = new RigidBoneSystemInputs(rigidBaseInputs);
		rigidBoneSystem.Bones[0].SetRotation(rigidInputs, Quaternion.RotationYawPitchRoll(0.1f, 0.2f, 0.3f));
		rigidBoneSystem.Bones[1].SetRotation(rigidInputs, Quaternion.RotationYawPitchRoll(0.2f, 0.3f, 0.4f));
		rigidBoneSystem.Bones[2].SetRotation(rigidInputs, Quaternion.RotationYawPitchRoll(0.3f, 0.4f, 0.5f));

		var inputs = new ChannelInputs(baseInputs);
		rigidBoneSystem.WriteInputs(inputs, baseOutputs, rigidInputs);
		var outputs = channelSystem.Evaluate(null, inputs);
		
		var baseTransforms = boneSystem.GetBoneTransforms(baseOutputs);
		var transforms = boneSystem.GetBoneTransforms(outputs);

		var rigidBaseTransforms = rigidBoneSystem.GetBoneTransforms(rigidBaseInputs);
		var rigidTransforms = rigidBoneSystem.GetBoneTransforms(rigidInputs);

		for (int transformIdx = 0; transformIdx < transforms.Length; ++transformIdx) {
			var baseTransform = baseTransforms[transformIdx];
			var transform = transforms[transformIdx];

			var rigidBaseTransform = rigidBaseTransforms[transformIdx];
			var rigidTransform = rigidTransforms[transformIdx];

			foreach (var testPoint in new [] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ }) {
				var unposedPoint = baseTransform.InverseTransform(testPoint);
				var posedPoint = transform.Transform(unposedPoint);

				var unposedRigidPoint = rigidBaseTransform.InverseTransform(testPoint);
				var posedRigidPoint = rigidTransform.Transform(unposedRigidPoint);
				
				float distance = Vector3.Distance(posedPoint, posedRigidPoint);
				Assert.AreEqual(0, distance, 1e-3);
			}
		}
	}
}
