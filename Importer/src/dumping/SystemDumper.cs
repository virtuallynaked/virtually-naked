using SharpDX;
using System;
using System.IO;

public class SystemDumper {
	private readonly Figure figure;
	private readonly SurfaceProperties surfaceProperties;
	private readonly bool[] channelsToInclude;
	private readonly DirectoryInfo figureDestDir;

	public SystemDumper(Figure figure, SurfaceProperties surfaceProperties, bool[] channelsToInclude, DirectoryInfo figureDestDir) {
		this.figure = figure;
		this.surfaceProperties = surfaceProperties;
		this.channelsToInclude = channelsToInclude;
		this.figureDestDir = figureDestDir;
	}

	private void Dump<T>(string filename, Func<T> factoryFunc) {
		var fileInfo = figureDestDir.File(filename);

		if (fileInfo.Exists) {
			return;
		}

		T obj = factoryFunc();
		
		figureDestDir.CreateWithParents();
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
		figureDestDir.CreateWithParents();
		Persistance.Save(figureDestDir.File("surface-properties.dat"), surfaceProperties);
		
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

	public static void DumpFigure(Figure figure, SurfaceProperties surfaceProperties, bool[] channelsToInclude, DirectoryInfo figureDestDir) {
		new SystemDumper(figure, surfaceProperties, channelsToInclude, figureDestDir).DumpAll();
	}
}
