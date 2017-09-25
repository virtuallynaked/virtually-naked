using SharpDX;
using System.IO;

public class FigureBehaviour {
	public static FigureBehaviour Load(ControllerManager controllerManager, IArchiveDirectory figureDir, FigureModel model) {
		InverterParameters inverterParameters = Persistance.Load<InverterParameters>(figureDir.File("inverter-parameters.dat"));
		return new FigureBehaviour(controllerManager, model, inverterParameters);
	}

	private const float FramesPerSecond = 30 * FigureActiveSettings.AnimationSpeed;
	
	private readonly FigureModel model;
	private readonly Poser poser;
	private readonly InverseKinematicsAnimator ikAnimator;
	private readonly IProceduralAnimator proceduralAnimator;
	private readonly DragHandle dragHandle;

	public FigureBehaviour(ControllerManager controllerManager, FigureModel model, InverterParameters inverterParameters) {
		this.model = model;
		poser = new Poser(model.ChannelSystem, model.BoneSystem);
		ikAnimator = new InverseKinematicsAnimator(controllerManager, model, inverterParameters);
		proceduralAnimator = new StandardProceduralAnimator(model);
		dragHandle = new DragHandle(controllerManager, FigureActiveSettings.InitialTransform);
	}
	
	private Pose GetBlendedPose(float time) {
		var posesByFrame = model.Animation.ActiveAnimation.PosesByFrame;

		float unloopedFrameIdx = time * FramesPerSecond;
 		float currentFrameIdx = unloopedFrameIdx % posesByFrame.Count;

		int baseFrameIdx = (int) currentFrameIdx;
		Pose prevFramePose = posesByFrame[IntegerUtils.Mod(baseFrameIdx + 0, posesByFrame.Count)];
		Pose nextFramePose = posesByFrame[IntegerUtils.Mod(baseFrameIdx + 1, posesByFrame.Count)];
		
		var poseBlender = new PoseBlender(model.BoneSystem.Bones.Count);
		float alpha = currentFrameIdx - baseFrameIdx;
		poseBlender.Add(1 - alpha, prevFramePose);
		poseBlender.Add(alpha, nextFramePose);
		var blendedPose = poseBlender.GetResult();
		return blendedPose;
	}

	public ChannelInputs Update(FrameUpdateParameters updateParameters, ControlVertexInfo[] previousFrameControlVertexInfos) {
		ChannelInputs inputs = new ChannelInputs(model.Inputs);

		dragHandle.Update();
		DualQuaternion rootTransform = DualQuaternion.FromMatrix(dragHandle.Transform);
		
		var blendedPose = GetBlendedPose(updateParameters.Time);
		poser.Apply(inputs, blendedPose, rootTransform);

		ikAnimator.Update(inputs, previousFrameControlVertexInfos);

		proceduralAnimator.Update(updateParameters, inputs);

		return inputs;
	}
}
