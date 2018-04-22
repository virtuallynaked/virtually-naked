using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class EnvironmentCubeGenerator {
	private static string FullNameWithoutExtension(FileInfo file, string implicitExtension) {
		if (file.Extension != implicitExtension) {
			throw new InvalidOperationException("file has wrong extension: " + file.Extension);
		}
		return Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.Name));
	}
	
	private static string Quote(string str) {
		return "\"" + str + "\"";
	}

	private static void RunCmft(FileInfo sourceFile, FileInfo destFile, double exponent) {
		List<string> arguments = new List<string>() {
			"--input " + Quote(sourceFile.FullName),
			"--output1 " + Quote(FullNameWithoutExtension(destFile, ".dds")),
			"--output1params dds,rgba32f,cubemap",
			"--srcFaceSize 128",
			"--mipCount 1",
			"--glossScale 0",
			"--useOpenCL true",
			"--deviceType gpu",
			"--numCpuProcessingThreads 0"
		};

		if (Double.IsInfinity(exponent)) {
			arguments.Add("--filter none");
		} else {
			double glossBias = Math.Log(exponent, 2);
			arguments.Add("--filter radiance");
			arguments.Add("--glossBias " + glossBias);
		}

		ProcessStartInfo startInfo = new ProcessStartInfo {
			FileName = @"third-party\cmft\cmft.exe",
			Arguments = String.Join(" ", arguments),
			UseShellExecute = false
		};

		Process process = Process.Start(startInfo);
		process.WaitForExit();
		if (process.ExitCode != 0) {
			throw new InvalidOperationException("cmft failed");
		}
	}

	private static void RunTexAssemble(List<FileInfo> sourceFiles, FileInfo destFile) {
		List<string> arguments = new List<string>() {
			"cubearray",
			"-y",
			"-o " + Quote(destFile.FullName)
		};
		arguments.AddRange(sourceFiles.Select(sourceFile => Quote(sourceFile.FullName)));

		ProcessStartInfo startInfo = new ProcessStartInfo {
			FileName = @"third-party\DirectXTex\texassemble.exe",
			Arguments = String.Join(" ", arguments),
			UseShellExecute = false
		};

		Process process = Process.Start(startInfo);
		process.WaitForExit();
		if (process.ExitCode != 0) {
			throw new InvalidOperationException("texassemble failed");
		}
	}

	private static void RunTexConv(FileInfo file) {
		List<string> arguments = new List<string>() {
			"-y",
			"-f BC6H_UF16",
			"-o " + Quote(file.DirectoryName),
			Quote(file.FullName)
		};

		ProcessStartInfo startInfo = new ProcessStartInfo {
			FileName = @"third-party\DirectXTex\texconv.exe",
			Arguments = String.Join(" ", arguments),
			UseShellExecute = false
		};

		Process process = Process.Start(startInfo);
		process.WaitForExit();
		if (process.ExitCode != 0) {
			throw new InvalidOperationException("texconv failed");
		}
	}

	private static void Rename(FileInfo sourceFile, FileInfo destFile) {
		if (destFile.Exists) {
			destFile.Delete();
		}
		File.Move(sourceFile.FullName, destFile.FullName);
	}

	private const int RoughnessLevels = 10;

	private void Generate(FileInfo sourceFile, DirectoryInfo destDir) {
		var destDiffuseFile = destDir.File("diffuse.dds");
		var destGlossyFile = destDir.File("glossy.dds");
		if (destDiffuseFile.Exists && destGlossyFile.Exists) {
			//environment was already imported
			return;
		}

		destDir.CreateWithParents();
		
		FileInfo destDiffuseUncompressed = destDir.File("diffuse-uncompressed.dds");
		RunCmft(sourceFile, destDiffuseUncompressed, 1);
		RunTexConv(destDiffuseUncompressed);
		Rename(destDiffuseUncompressed, destDiffuseFile);

		List<FileInfo> glossyLevels = new List<FileInfo>();
		for (int roughnessIdx = 0; roughnessIdx <= RoughnessLevels; ++roughnessIdx) {
			double roughness = (double) roughnessIdx / RoughnessLevels;
			double exponent = roughness == 0 ? Double.PositiveInfinity : 1 / (2 * Math.Pow(roughness, 4));
			
			FileInfo destGlossyLevel = destDir.File($"glossy-{roughnessIdx}.dds");
			RunCmft(sourceFile, destGlossyLevel, exponent);
			glossyLevels.Add(destGlossyLevel);
		}

		FileInfo destGlossyUncompressed = destDir.File("glossy-uncompressed.dds");
		RunTexAssemble(glossyLevels, destGlossyUncompressed);
		RunTexConv(destGlossyUncompressed);
		Rename(destGlossyUncompressed, destGlossyFile);
		
		glossyLevels.ForEach(file => file.Delete());
	}

	public void Run(ImportSettings importSettings) {
		DirectoryInfo sourceEnvironmentsDir = CommonPaths.SourceAssetsDir.Subdirectory("environments");

		foreach (FileInfo sourceFile in sourceEnvironmentsDir.EnumerateFiles()) {
			string environmentName = Path.GetFileNameWithoutExtension(sourceFile.Name);
			if (!importSettings.ShouldImportEnvironment(environmentName)) {
				continue;
			}

			DirectoryInfo destDir = CommonPaths.WorkDir.Subdirectory("environments").Subdirectory(environmentName);
			Generate(sourceFile, destDir);
		}
	}
}
