using System.IO;
using System.Linq;
using System.Collections.Generic;

class AnimationDumper {
	private static readonly DirectoryInfo AnimationSourceDirectory = CommonPaths.SourceAssetsDir.Subdirectory("mixamo-animations");

	public static void DumpAllAnimations(ImporterPathManager pathManager, Figure figure) {
		AnimationDumper dumper = new AnimationDumper(pathManager, figure);
		foreach (FileInfo animationSourceFile in AnimationSourceDirectory.EnumerateFiles("*.dae")) {
			string animationName = Path.GetFileNameWithoutExtension(animationSourceFile.Name);
			dumper.Dump(animationName, animationSourceFile);
		}
	}
	
	private readonly Figure figure;

	private readonly DirectoryInfo animationsDirectory;
	private readonly ChannelOutputs orientationOutputs;
	
	public AnimationDumper(ImporterPathManager pathManager, Figure figure) {
		this.figure = figure;

		var figureDirectory = pathManager.GetDestDirForFigure(figure.Name);
		this.animationsDirectory = figureDirectory.Subdirectory("animations");
		this.orientationOutputs = figure.ChannelSystem.DefaultOutputs; //orientation doesn't seem to change between actors so we can use default outputs
	}
	
	private List<Pose> LoadPoses(FileInfo sourceFile) {
		var importer = Mixamo.AnimationImporter.MakeFromFilename(sourceFile.FullName);
		int frameCount = importer.FrameCount;

		var figurePoses = Enumerable.Range(0, frameCount)
			.Select(frameIdx => {
				var mixamoPose = importer.Import(frameIdx);
				var pose = new Mixamo.AnimationRetargeter(figure.BoneSystem, orientationOutputs).Retarget(mixamoPose);
				return pose;
			})
			.ToList();

		return figurePoses;
	}

	public void Dump(string name, FileInfo sourceFile) {
		FileInfo animationFile = animationsDirectory.File(name + ".dat");
		if (animationFile.Exists) {
			return;
		}

		var frameInputs = LoadPoses(sourceFile);
		
		//persist
		animationFile.Directory.CreateWithParents();
		Persistance.Save(animationFile, frameInputs);
    }
}
