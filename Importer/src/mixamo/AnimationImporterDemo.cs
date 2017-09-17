using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixamo {
	public class AnimationImporterDemo {
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
