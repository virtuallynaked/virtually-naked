using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Animation {
	public string Label { get; }
	public List<Pose> PosesByFrame { get; }

	public static Animation Load(IArchiveFile animationFile) {
		string label = Path.GetFileNameWithoutExtension(animationFile.Name);
		List<Pose> posesByFrame = Persistance.Load<List<Pose>>(animationFile);
		return new Animation(label, posesByFrame);
	}

	public static Animation MakeNone(BoneSystem boneSystem) {
		string label = "None";
		List<Pose> posesByFrame = new List<Pose> { Pose.MakeIdentity(boneSystem.Bones.Count) };
		return new Animation(label, posesByFrame);
	}

	public Animation(string label, List<Pose> posesByFrame) {
		Label = label;
		PosesByFrame = posesByFrame;
	}
}

public class AnimationMenuItem : IToggleMenuItem {
	private readonly AnimationModel model;
	private readonly Animation animation;

	public AnimationMenuItem(AnimationModel model, Animation animation) {
		this.model = model;
		this.animation = animation;
	}

	public string Label => animation.Label;

	public bool IsSet => model.ActiveAnimation == animation;

	public void Toggle() {
		model.ActiveAnimation = animation;
	}
}

public class AnimationMenuLevel : IMenuLevel {
	private readonly AnimationModel model;

	public AnimationMenuLevel(AnimationModel model) {
		this.model = model;
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return model.Animations
			.Select(animation => (IMenuItem) new AnimationMenuItem(model, animation))
			.ToList();
	}
}

public class AnimationModel {
	public static AnimationModel Load(IArchiveDirectory figureDir, BoneSystem boneSystem, string startingAnimationName) {
		List<Animation> animations = new List<Animation>();
		Animation activeAnimation = null;

		Animation noneAnimation = Animation.MakeNone(boneSystem);
		animations.Add(noneAnimation);
		activeAnimation = noneAnimation;

		IArchiveDirectory animationDir = figureDir.Subdirectory("animations");
		if (animationDir != null) {
			foreach (IArchiveFile animationFile in figureDir.Subdirectory("animations").GetFiles()) {
				Animation animation = Animation.Load(animationFile);
				animations.Add(animation);
				if (animation.Label == startingAnimationName) {
					activeAnimation = animation;
				}
			}
		}
		
		return new AnimationModel(animations, activeAnimation);
	}

	public List<Animation> Animations { get; }
	public Animation ActiveAnimation { get; set; }
	
	public AnimationModel(List<Animation> animations, Animation activeAnimation) {
		this.Animations = animations;
		this.ActiveAnimation = activeAnimation;
	}
	
	public string ActiveName {
		get {
			return ActiveAnimation.Label;
		}
		set {
			ActiveAnimation = Animations.Find(option => option.Label == value);
		}
	}
}
