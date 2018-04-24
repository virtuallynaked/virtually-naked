using Newtonsoft.Json;
using System.Collections.Generic;
using SharpDX;

public class ActorBehavior {
	public static ActorBehavior Load(ControllerManager controllerManager, IArchiveDirectory figureDir, ActorModel model) {
		InverterParameters inverterParameters = Persistance.Load<InverterParameters>(figureDir.File("inverter-parameters.dat"));
		return new ActorBehavior(controllerManager, model, inverterParameters);
	}

	private const float FramesPerSecond = 30 * InitialSettings.AnimationSpeed;
	
	private readonly ActorModel model;
	private readonly Poser poser;
	private readonly InverseKinematicsAnimator ikAnimator;
	private readonly IProceduralAnimator proceduralAnimator;
	private readonly DragHandle dragHandle;

	public ActorBehavior(ControllerManager controllerManager, ActorModel model, InverterParameters inverterParameters) {
		this.model = model;
		poser = new Poser(model.MainDefinition);
		ikAnimator = new InverseKinematicsAnimator(controllerManager, model.MainDefinition, inverterParameters);
		proceduralAnimator = new StandardProceduralAnimator(model.MainDefinition, model.Behavior);
		dragHandle = new DragHandle(controllerManager, InitialSettings.InitialTransform);

		model.PoseReset += ikAnimator.Reset;
	}
	
	private Pose GetBlendedPose(float time) {
		var posesByFrame = model.Animation.ActiveAnimation.PosesByFrame;

		float unloopedFrameIdx = time * FramesPerSecond;
 		float currentFrameIdx = unloopedFrameIdx % posesByFrame.Count;

		int baseFrameIdx = (int) currentFrameIdx;
		Pose prevFramePose = posesByFrame[IntegerUtils.Mod(baseFrameIdx + 0, posesByFrame.Count)];
		Pose nextFramePose = posesByFrame[IntegerUtils.Mod(baseFrameIdx + 1, posesByFrame.Count)];
		
		var poseBlender = new PoseBlender(model.MainDefinition.BoneSystem.Bones.Count);
		float alpha = currentFrameIdx - baseFrameIdx;
		poseBlender.Add(1 - alpha, prevFramePose);
		poseBlender.Add(alpha, nextFramePose);
		var blendedPose = poseBlender.GetResult();
		return blendedPose;
	}

	public ChannelInputs Update(ChannelInputs shapeInputs, FrameUpdateParameters updateParameters, ControlVertexInfo[] previousFrameControlVertexInfos) {
		ChannelInputs inputs = new ChannelInputs(shapeInputs);
		
		for (int idx = 0; idx < inputs.RawValues.Length; ++idx) {
			double initialValue = model.MainDefinition.ChannelSystem.Channels[idx].InitialValue;
			inputs.RawValues[idx] += (model.Inputs.RawValues[idx] - initialValue);
		}

		dragHandle.Update(updateParameters);
		DualQuaternion rootTransform = DualQuaternion.FromMatrix(dragHandle.Transform);
		
		var blendedPose = GetBlendedPose(updateParameters.Time);
		poser.Apply(inputs, blendedPose, rootTransform);

		ikAnimator.Update(updateParameters, inputs, previousFrameControlVertexInfos);

		proceduralAnimator.Update(updateParameters, inputs);

		return inputs;
	}

	public class PoseRecipe {
		[JsonProperty("rotation")]
		public float[] rotation;

		[JsonProperty("translation")]
		public float[] translation;

		[JsonProperty("bone-rotations")]
		public Dictionary<string, float[]> boneRotations;

		public void Merge(ActorBehavior behaviour) {
			Vector3 rootRotation = new Vector3(rotation);
			Vector3 rootTranslation = new Vector3(translation);
			DualQuaternion rootTransform = DualQuaternion.FromRotationTranslation(
				behaviour.model.MainDefinition.BoneSystem.RootBone.RotationOrder.FromEulerAngles(MathExtensions.DegreesToRadians(rootRotation)),
				rootTranslation);
			behaviour.dragHandle.Transform = rootTransform.ToMatrix();

			var poseDeltas = behaviour.ikAnimator.PoseDeltas;
			poseDeltas.ClearToZero();
			foreach (var bone in behaviour.model.MainDefinition.BoneSystem.Bones) {
				Vector3 angles;
				if (boneRotations.TryGetValue(bone.Name, out var values)) {
					angles = new Vector3(values);
				} else {
					angles = Vector3.Zero;
				}
				var twistSwing = bone.RotationOrder.FromTwistSwingAngles(MathExtensions.DegreesToRadians(angles));
				poseDeltas.Rotations[bone.Index] = twistSwing;
			}
		}
	}

	public PoseRecipe RecipizePose() {
		var rootTransform = DualQuaternion.FromMatrix(dragHandle.Transform);
		Vector3 rootRotation = MathExtensions.RadiansToDegrees(model.MainDefinition.BoneSystem.RootBone.RotationOrder.ToEulerAngles(rootTransform.Rotation));
		Vector3 rootTranslation = rootTransform.Translation;
		
		Dictionary<string, float[]> boneRotations = new Dictionary<string, float[]>();
		var poseDeltas = ikAnimator.PoseDeltas;

		rootTranslation += Vector3.Transform(poseDeltas.RootTranslation / 100, rootTransform.Rotation);
		foreach (var bone in model.MainDefinition.BoneSystem.Bones) {
			var twistSwing = poseDeltas.Rotations[bone.Index];
			var angles = MathExtensions.RadiansToDegrees(bone.RotationOrder.ToTwistSwingAngles(twistSwing));
			if (!angles.IsZero) {
				boneRotations.Add(bone.Name, angles.ToArray());
			}
		}

		return new PoseRecipe {
			rotation = rootRotation.ToArray(),
			translation = rootTranslation.ToArray(),
			boneRotations = boneRotations
		};
	}
}
