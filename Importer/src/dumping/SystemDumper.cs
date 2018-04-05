using SharpDX;
using System;
using System.IO;

public class SystemDumper {
	private Figure figure;
	private bool[] channelsToInclude;
	private DirectoryInfo targetDirectory;

	public SystemDumper(Figure figure, bool[] channelsToInclude) {
		this.figure = figure;
		this.channelsToInclude = channelsToInclude;

		this.targetDirectory = CommonPaths.WorkDir.Subdirectory("figures")
			.Subdirectory(figure.Name);
	}

	private void Dump<T>(string filename, Func<T> factoryFunc) {
		var fileInfo = targetDirectory.File(filename);

		if (fileInfo.Exists) {
			return;
		}

		T obj = factoryFunc();
		
		targetDirectory.CreateWithParents();
		Persistance.Save(fileInfo, obj);
	}
	
	private void ValidateBoneSystemAssumptions(Figure figure) {
		var outputs = figure.ChannelSystem.DefaultOutputs;

		foreach (var bone in figure.Bones) {
			var centerPoint = bone.CenterPoint.GetValue(outputs);
			var endPoint = bone.EndPoint.GetValue(outputs);
			var orientationSpace = bone.GetOrientationSpace(outputs);

			var boneDirection = Vector3.Transform(endPoint - centerPoint, orientationSpace.OrientationInverse);
			
			int twistAxis = 0;
			for (int i = 1; i < 3; ++i) {
				if (Math.Abs(boneDirection[i]) > Math.Abs(boneDirection[twistAxis])) {
					twistAxis = i;
				}
			}
			
			if (twistAxis != bone.RotationOrder.primaryAxis) {
				throw new Exception("twist axis is not primary axis");
			}
		}
	}

	public void DumpAll() {
		var surfaceProperties = SurfacePropertiesJson.Load(figure);
		targetDirectory.CreateWithParents();
		Persistance.Save(targetDirectory.File("surface-properties.dat"), surfaceProperties);
		
		Dump("shaper-parameters.dat", () => figure.MakeShaperParameters(channelsToInclude));
		Dump("channel-system-recipe.dat", () => figure.MakeChannelSystemRecipe());

		if (figure.Parent == null) {
			ValidateBoneSystemAssumptions(figure);
			Dump("bone-system-recipe.dat", () => figure.MakeBoneSystemRecipe());
			Dump("inverter-parameters.dat", () => figure.MakeInverterParameters());
		} else {
			Dump("child-to-parent-bind-pose-transforms.dat", () => figure.ChildToParentBindPoseTransforms);
		}
	}

	public static void DumpFigure(Figure figure, bool[] channelsToInclude) {
		new SystemDumper(figure, channelsToInclude).DumpAll();
	}
}
