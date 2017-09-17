using Nvidia.TextureTools;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public enum TextureProcessingType {
	Color, SingleChannel, Normal, Bump
};

class TextureProcessingSettings {
	private readonly FileInfo file;
	private TextureProcessingType type;
	private readonly bool isLinear;
	private readonly TextureMask mask;

	public TextureProcessingSettings(FileInfo file, TextureProcessingType type, bool isLinear, TextureMask mask) {
		this.file = file;
		this.type = type;
		this.isLinear = isLinear;
		this.mask = mask;
	}

	public FileInfo File => file;
	public TextureProcessingType Type => type;
	public bool IsLinear => isLinear;
	public TextureMask Mask => mask;

	public static bool CanUpgrade(TextureProcessingType fromType, TextureProcessingType toType) {
		if (fromType == toType) {
			return true;
		}

		if (fromType == TextureProcessingType.SingleChannel && toType == TextureProcessingType.Color) {
			return true;
		}

		return false;
	}

	public static TextureProcessingType MergeType(TextureProcessingType typeA, TextureProcessingType typeB) {
		if (CanUpgrade(typeA, typeB)) {
			return typeB;
		}
		if (CanUpgrade(typeB, typeA)) {
			return typeA;
		}

		throw new InvalidOperationException("texture type conflict");
	}

	public void Merge(FileInfo file, TextureProcessingType type, bool isLinear, TextureMask mask) {
		if (this.file.FullName != file.FullName) {
			throw new InvalidOperationException("texture file conflict");
		}

		this.type = MergeType(this.type, type);
		
		if (this.isLinear != isLinear) {
			throw new InvalidOperationException("texture isLinear conflict");
		}

		this.mask.Merge(mask);
	}
}

public class TextureProcessor {
	private static readonly bool Compress = false;
	
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly DirectoryInfo destinationFolder;
	
	private readonly Dictionary<string, TextureProcessingSettings> settingsByName = new Dictionary<string, TextureProcessingSettings>();

	public TextureProcessor(Device device, ShaderCache shaderCache, DirectoryInfo destinationFolder) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.destinationFolder = destinationFolder;
	}

	private static void CompressTexture(FileInfo file, TextureProcessingType type, bool isLinear) {
		string compressionFormat;
		if (type == TextureProcessingType.SingleChannel && isLinear) {
			compressionFormat = "BC4_UNORM";
		} else {
			compressionFormat = isLinear ? "BC7_UNORM" : "BC7_UNORM_SRGB";
		}

		List<string> arguments = new List<string>() {
			"-y",
			isLinear ? "" : "-srgb",
			"-f " + compressionFormat,
			"-bcmax",
			file.Name
		};

		ProcessStartInfo startInfo = new ProcessStartInfo {
			WorkingDirectory = file.DirectoryName,
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

	private void ImportTexture(string name, TextureProcessingSettings settings) {
		string destinationPath = Path.Combine(destinationFolder.FullName, name + ".dds");
		if (new FileInfo(destinationPath).Exists) {
			return;
		}

		Console.WriteLine($"importing texture '{name}'...");
		
		using (var image = UnmanagedRgbaImage.Load(settings.File))  {
			using (var dilator = new TextureDilator(device, shaderCache)) {
				dilator.Dilate(settings.Mask, image.Size, settings.IsLinear, image.DataBox);
			}

			InputOptions input = new InputOptions();
			input.SetFormat(InputFormat.BGRA_8UB);
			input.SetTextureLayout(TextureType.Texture2D, image.Size.Width, image.Size.Height, 1);
			float gamma = settings.IsLinear ? 1f : 2.2f;
			input.SetGamma(gamma, gamma);
			input.SetMipmapData(image.PixelData, image.Size.Width, image.Size.Height, 1, 0, 0);
			input.SetAlphaMode(AlphaMode.None);

			input.SetMipmapGeneration(true);
			input.SetMipmapFilter(MipmapFilter.Kaiser);
			input.SetKaiserParameters(3, 4, 1);

			if (settings.Type == TextureProcessingType.Bump) {
				input.SetConvertToNormalMap(true);
				input.SetNormalFilter(1, 0, 0, 0);
				input.SetHeightEvaluation(1, 1, 1, 0);
			} else if (settings.Type == TextureProcessingType.Normal) {
				input.SetNormalMap(true);
			}

			CompressionOptions compression = new CompressionOptions();
			compression.SetQuality(Quality.Highest);
			compression.SetFormat(Format.RGBA);

			OutputOptions output = new OutputOptions();
			destinationFolder.CreateWithParents();
			output.SetFileName(destinationPath);
			output.SetContainer(Container.Container_DDS10);
			output.SetSrgbFlag(!settings.IsLinear);
			
			var compressor = new Compressor();
			bool succeeded = compressor.Compress(input, compression, output);
			if (!succeeded) {
				throw new InvalidOperationException("texture conversion failed");
			}
			
			//force the previous output handler to be destructed so that the file is flushed and closed
			output.SetFileName("nul");

			if (Compress) {
				CompressTexture(new FileInfo(destinationPath), settings.Type, settings.IsLinear);
			}
		}
	}

	public string RegisterForProcessing(FileInfo textureFile, TextureProcessingType type, bool isLinear, TextureMask mask) {
		string name = Path.GetFileNameWithoutExtension(textureFile.FullName);
		if (type == TextureProcessingType.Bump) {
			name += "-to-normal";
		}

		if (!settingsByName.TryGetValue(name, out var settings)) {
			settings = new TextureProcessingSettings(textureFile, type, isLinear, mask);
			settingsByName.Add(name, settings);
		} else {
			settings.Merge(textureFile, type, isLinear, mask);
		}

		return name;
	}

	public void ImportAll() {
		foreach (var entry in settingsByName) {
			ImportTexture(entry.Key, entry.Value);
		}
	}
}
