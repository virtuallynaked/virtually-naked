using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

public class ManifestFile {
    [XmlAttribute("VALUE")]
    public string Path { get; set; }
}

[XmlRoot("DAZInstallManifest")]
public class Manifest {
    [XmlElement("File")]
    public List<ManifestFile> Files { get; set; }

    public static XmlSerializer Serializer = new XmlSerializer(typeof(Manifest));
}

public class ContentFileLocator {
    private static readonly DirectoryInfo DazAssetsDir = CommonPaths.SourceAssetsDir.Subdirectory("daz-assets");
	private static readonly DirectoryInfo DazAssetPatchesDir = CommonPaths.SourceAssetsDir.Subdirectory("daz-asset-patches");
	
	private static readonly Dictionary<string, string> PathCorrections = new Dictionary<string, string> {
		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Expressions/PHMNoseCompressionHD.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMNoseCompressionHD.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Expressions/PHMBrowCompressionHD.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMBrowCompressionHD.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Expressions/PHMCheeksDimpleCreaseHDL.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMCheeksDimpleCreaseHDL.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Expressions/CTRLCheeksDimpleCreaseHD.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/CTRLCheeksDimpleCreaseHD.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Expressions/PHMCheeksDimpleCreaseHDR.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMCheeksDimpleCreaseHDR.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Heads/CTRLCrowsFeetHD.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/CTRLCrowsFeetHD.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Heads/PHMCrowsFeetHDR.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMCrowsFeetHDR.dsf",

		["/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Heads/PHMCrowsFeetHDL.dsf"]
			= "/data/DAZ 3D/Genesis 3/Female/Morphs/DAZ 3D/Head/PHMCrowsFeetHDL.dsf"
	};

    private readonly Dictionary<string, string> contentLocations = new Dictionary<string, string>();

    public ContentFileLocator() {
        foreach (DirectoryInfo contentPackageDirectory in DazAssetsDir.GetDirectories()) {
			ParseManifest(contentPackageDirectory);
        }
		ImportPatches(DazAssetPatchesDir);
    }
    
    private void ParseManifest(DirectoryInfo contentPackageDirectory) {
        string manifestPath = Path.Combine(contentPackageDirectory.FullName, "Manifest.dsx");
        
		if (!File.Exists(manifestPath)) {
			string directoryPath = contentPackageDirectory.FullName;

			FileInfo[] contentFiles = contentPackageDirectory.GetFiles("*", SearchOption.AllDirectories);
			foreach (FileInfo contentFile in contentFiles) {
				string fullPath = contentFile.FullName;
				if (!fullPath.StartsWith(contentPackageDirectory.FullName)) {
					throw new InvalidOperationException("unexpected content file not inside content directory: " + fullPath);
				}
				string relativePath = fullPath.Substring(contentPackageDirectory.FullName.Length).Replace("\\", "/").ToLowerInvariant();
				contentLocations[relativePath] = fullPath;
			}
			return;
		}

        Manifest manifest = (Manifest)Manifest.Serializer.Deserialize(File.OpenRead(manifestPath));

        foreach (ManifestFile file in manifest.Files) {
            string path = file.Path;
            int indexOfSlash = path.IndexOf('/');
            string prefix = path.Substring(0, indexOfSlash);
            string suffix = path.Substring(indexOfSlash + 1);

            if (prefix != "Content") {
                throw new InvalidOperationException("unexpected folder in manifest: " + prefix);
            }

            contentLocations["/" + suffix.ToLowerInvariant()] = Path.Combine(contentPackageDirectory.FullName, path);
        }
    }

	private void ImportPatches(DirectoryInfo patchDirectory) {
		foreach (FileInfo file in patchDirectory.GetFiles()) {
			contentLocations["/patches/" + file.Name] = file.FullName;
		}
	}

	public string Locate(string path, bool throwIfMissing = true) {
		if (PathCorrections.TryGetValue(path, out var correctedPath)) {
			path = correctedPath;
		}

        if (!contentLocations.TryGetValue(path.ToLowerInvariant(), out string fullPath)) {
			if (throwIfMissing) {
	            throw new InvalidOperationException("missing content file: " + path);
			}
        }
        return fullPath;
    }

	public IEnumerable<string> GetAllUnderPath(string path) {
		string lowerCastPath = path.ToLowerInvariant();

		return contentLocations
			.Where(entry => entry.Key.StartsWith(lowerCastPath))
			.Select(entry => entry.Key);
	}
}
