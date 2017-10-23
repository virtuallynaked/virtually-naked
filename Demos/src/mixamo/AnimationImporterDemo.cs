using System;

namespace Mixamo {
	public class AnimationImporterDemo : IDemoApp {
		public void Run() {
			string filename = @"C:\Users\Ted\Documents\Projects\DazPoser\source-assets\Female_Standing_Pose.xml";
			//string filename = @"C:\Users\Ted\Documents\Projects\DazPoser\source-assets\t_pose.xml";

			var importer = AnimationImporter.MakeFromFilename(filename);
			var pose = importer.Import(-1);

			Console.WriteLine("root translation: " + pose.RootTranslation);
			foreach (var entry in pose.JointRotations) {
				Console.WriteLine(entry.Key + ": " + entry.Value);
			}
		}
	}
}
