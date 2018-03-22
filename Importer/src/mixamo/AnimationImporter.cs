using ColladaTypes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mixamo {
	public class AnimationImporter {
		private readonly ColladaRoot root;
		private readonly Dictionary<string, Matrix> inverseBindPosesByJointName = new Dictionary<string, Matrix>();
		private readonly Dictionary<string, Matrix> bindPosesByJointName = new Dictionary<string, Matrix>();
		private readonly Dictionary<string, Matrix[]> animationPosesByJointName = new Dictionary<string, Matrix[]>();

		public static AnimationImporter MakeFromFilename(string filename) {
			ColladaRoot root = (ColladaRoot) ColladaRoot.Serializer.Deserialize(File.OpenRead(filename));
			return new AnimationImporter(root);
		}

		public AnimationImporter(ColladaRoot root) {
			this.root = root;
			LoadBindPoses();
			LoadAnimationPoses();
		}

		public int FrameCount => animationPosesByJointName.Values.First().Length;

		public MixamoPose Import(int frameIdx) {
			Node rootJoint = root.VisualSceneLibrary.VisualScenes[0].Nodes.Find(node => node.Type == "JOINT");

			var jointRotations = new Dictionary<string, Quaternion>();
			var rootTranslation = Vector3.Zero;
			ImportJoints(frameIdx, jointRotations, ref rootTranslation, rootJoint, null);
			return new MixamoPose(jointRotations, rootTranslation);
		}

		private void LoadBindPoses() {
			//Assumes bind_shape_matrix is Identity

			List<Source> controllerSources = root.ControllerLibrary.Controllers[0].Skin.Sources;
			
			string[] jointNames = controllerSources.Find(s => s.Id.EndsWith("-Joints")).NameArray
				.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			Matrix[] inverseBindPoses = ColladaUtils.MatricesFromString(controllerSources.Find(s => s.Id.EndsWith("-Matrices")).FloatArray);

			if (inverseBindPoses.Length != jointNames.Length) {
				throw new InvalidOperationException("expected equal number of bind poses and weights");
			}

			for (int i = 0; i < jointNames.Length; ++i) {
				Matrix mat = inverseBindPoses[i];
				inverseBindPosesByJointName[jointNames[i]] = mat;
				mat.Invert();
				bindPosesByJointName[jointNames[i]] = mat;
			}
		}
		
		private void LoadAnimationPoses() {
			foreach (var animation in root.LibraryAnimations.Animations) {
				string jointName = animation.Name;
				Matrix[] poses = ColladaUtils.MatricesFromString(animation.Sources.Single(s => s.Id.EndsWith("-output-transform")).FloatArray);
				animationPosesByJointName[jointName] = poses;
			}
		}

		private static readonly Vector3 Epsilon = 1e-3f * Vector3.One;
		
		private void ImportJoints(int frameIdx, Dictionary<string, Quaternion> jointRotations, ref Vector3 rootTranslation, Node joint, Node parentJoint) {
			string jointName = joint.Name;

			if (!inverseBindPosesByJointName.TryGetValue(jointName, out Matrix invBindPose)) {
				return;
			}
			
			Matrix relAnimPose;
			if (frameIdx < 0) {
				relAnimPose = ColladaUtils.MatrixFromString(joint.Matrix);
			} else {
				relAnimPose = animationPosesByJointName[jointName][frameIdx];
			}
			
			Matrix bindPose = bindPosesByJointName[jointName];
			Matrix bindTranslation = Matrix.Translation(bindPose.TranslationVector);
			Matrix invBindTranslation = Matrix.Translation(-bindPose.TranslationVector);

			Matrix parentBindPose = parentJoint != null ? bindPosesByJointName[parentJoint.Name] : Matrix.Identity;
			
			Matrix invBindPoseWithoutTranslation = invBindPose;
			invBindPoseWithoutTranslation.TranslationVector = Vector3.Zero;

			Matrix local = invBindPoseWithoutTranslation * relAnimPose * parentBindPose * invBindTranslation;
			
			local.Decompose(out Vector3 localScale, out Quaternion localRotation, out Vector3 localTranslation);

			if (!Vector3.NearEqual(localScale, Vector3.One, Epsilon)) {
				throw new ArgumentException("local joint transform has a non-unity scale");
			}

			if (parentJoint != null) {
				if (!Vector3.NearEqual(localTranslation, Vector3.Zero, Epsilon)) {
					throw new ArgumentException("local joint transform has a non-zero translation");
				}
			} else {
				rootTranslation = localTranslation;
			}
			
			jointRotations[jointName] = localRotation;
			
			foreach (Node child in joint.Nodes) {
				ImportJoints(frameIdx, jointRotations, ref rootTranslation, child, joint);
			}
		}
	}
}
