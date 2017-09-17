using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace Mixamo {
	class AnimationRetargeter {
		private readonly BoneSystem boneSystem;
		private readonly ChannelOutputs outputs; 

		public AnimationRetargeter(BoneSystem boneSystem, ChannelOutputs outputs) {
			this.boneSystem = boneSystem;
			this.outputs = outputs;
		}

		private void SplitBendAndTwist(Vector3 axis, Quaternion sourceRotation, Dictionary<string, Quaternion> target, string bendName, string twistName) {
			OrientationSpace orientationSpace = boneSystem.BonesByName[twistName].GetOrientationSpace(outputs);

			orientationSpace.DecomposeIntoTwistThenSwing(axis, sourceRotation, out Quaternion twist, out Quaternion swing);

			target[twistName] = twist;
			target[bendName] = swing;
		}

		public Pose Retarget(MixamoPose sourcePose) {
			Dictionary<string, Quaternion> source = sourcePose.JointRotations;
			Dictionary<string, Quaternion> target = new Dictionary<string, Quaternion>() { };

			target["hip"] = source["mixamorig_Hips"];
			target["abdomenLower"] = source["mixamorig_Spine"];
			target["abdomenUpper"] = source["mixamorig_Spine1"];

			target["chestLower"] = source["mixamorig_Spine2"].Pow(2/3f);
			target["chestUpper"] = source["mixamorig_Spine2"].Pow(1/3f);
			
			target["neckLower"] = source["mixamorig_Neck"];

			target["neckUpper"] = source["mixamorig_Head"].Pow(0.5f);
			target["head"] = source["mixamorig_Head"].Pow(0.5f);
			
			Quaternion meanUpperLeg = Quaternion.Slerp(source[$"mixamorig_LeftUpLeg"], source[$"mixamorig_RightUpLeg"], 0.5f);
			Quaternion pelvis = meanUpperLeg.Pow(1/9f);
			Quaternion inversePelvis = Quaternion.Invert(pelvis);
			target["pelvis"] = pelvis;

			for (int side = 0; side < 2; ++side) {
				string l = side == 0 ? "l" : "r";
				string left = side == 0 ? "Left" : "Right";

				target[$"{l}Collar"] = source[$"mixamorig_{left}Shoulder"];
				
				SplitBendAndTwist(Vector3.UnitX,
					source[$"mixamorig_{left}Arm"],
					target, $"{l}ShldrBend", $"{l}ShldrTwist");

				SplitBendAndTwist(Vector3.UnitX,
					source[$"mixamorig_{left}ForeArm"],
					target, $"{l}ForearmBend", $"{l}ForearmTwist");
				
				target[$"{l}Hand"] = source[$"mixamorig_{left}Hand"];

				for (int i = 1; i <= 3; ++i) {
					target[$"{l}Thumb{i}"] = source[$"mixamorig_{left}HandThumb{i}"];
					target[$"{l}Index{i}"] = source[$"mixamorig_{left}HandIndex{i}"];
					target[$"{l}Mid{i}"] = source[$"mixamorig_{left}HandMiddle{i}"];
					target[$"{l}Ring{i}"] = source[$"mixamorig_{left}HandRing{i}"];
					target[$"{l}Pinky{i}"] = source[$"mixamorig_{left}HandPinky{i}"];
				}
				
				SplitBendAndTwist(Vector3.UnitY,
					source[$"mixamorig_{left}UpLeg"].Chain(inversePelvis),
					target, $"{l}ThighBend", $"{l}ThighTwist");
				
				target[$"{l}Shin"] = source[$"mixamorig_{left}Leg"];
				target[$"{l}Foot"] = source[$"mixamorig_{left}Foot"];
				
				target[$"{l}Toe"] = source[$"mixamorig_{left}ToeBase"];
			}
			
			var boneRotations = boneSystem.Bones
				.Select(bone => {
					if (target.TryGetValue(bone.Name, out var rotation)) {
						return rotation;
					} else {
						return Quaternion.Identity;
					}
				})
				.ToArray();
				
			return new Pose(sourcePose.RootTranslation, boneRotations);
		}
	}
}
