using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class UiImporter {
	private readonly DirectoryInfo contentDestDir;

	public UiImporter(DirectoryInfo contentDestDir) {
		this.contentDestDir = contentDestDir;
	}

	public void Run() {
		Import("put-on-headset-overlay");
	}

	private static string Quote(string str) {
		return "\"" + str + "\"";
	}

	private void Import(string name) {
		var destDir = contentDestDir.Subdirectory("ui");
		var destFile = destDir.File(name + ".dds");
		if (destFile.Exists) {
			return;
		}

		var sourceFile = CommonPaths.SourceAssetsDir.Subdirectory("ui").File(name + ".png");

		destDir.CreateWithParents();

		List<string> arguments = new List<string>() {
			"-y",
			"-f B8G8R8A8_UNORM_SRGB",
			"-srgb",
			"-m 1",
			"-pmalpha",
			"-o " + Quote(destDir.FullName),
			Quote(sourceFile.FullName)
		};

		ProcessStartInfo startInfo = new ProcessStartInfo {
			FileName = @"third-party\DirectXTex\texconv.exe",
			Arguments = String.Join(" ", arguments),
			UseShellExecute = false
		};

		Process process = Process.Start(startInfo);
		process.WaitForExit();
		if (process.ExitCode != 0) {
			destFile.Delete();
			throw new InvalidOperationException("texconv failed");
		}

		File.Move(destFile.FullName, destFile.FullName); //fixup case
	}
}
